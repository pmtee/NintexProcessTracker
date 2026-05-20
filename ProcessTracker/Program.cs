using Microsoft.EntityFrameworkCore;
using ProcessTracker;

// ── BUILDER ────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// I add Swagger so every endpoint is documented automatically
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "ProcessTracker API",
        Version     = "v2.0",
        Description = "Business process automation API — C# .NET 8, EF Core, SQLite, Docker, Kubernetes"
    });
});

// I add CORS so the dashboard UI can call the API from the browser
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// I configure SQLite — path works both locally and inside Docker
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "data", "ProcessTracker.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// I register the repository — anywhere IProcessRepository is needed
// .NET injects ProcessRepository automatically
builder.Services.AddScoped<IProcessRepository, ProcessRepository>();

// I register the AWS S3 export service
builder.Services.AddScoped<AwsS3Service>();

// I register the background automation service
// It runs every 60 seconds and auto-fails stalled processes
builder.Services.AddHostedService<ProcessTimeoutService>();

// ── APP ─────────────────────────────────────────────────────
var app = builder.Build();

// I create the database and all tables on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProcessTracker API v2");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseStaticFiles();

// ── HEALTH CHECK ────────────────────────────────────────────
// I confirm the API and database are both alive
// Kubernetes liveness and readiness probes call this endpoint
app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status      = "healthy",
            database    = "connected",
            timestamp   = DateTime.UtcNow,
            service     = "ProcessTracker API",
            version     = "2.0.0",
            environment = app.Environment.EnvironmentName
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "unhealthy", error = ex.Message },
            statusCode: 503);
    }
})
.WithName("HealthCheck")
.WithTags("System");

// ── GET /processes ──────────────────────────────────────────
// I return all processes — supports optional status filter
app.MapGet("/processes", async (IProcessRepository repo, string? status) =>
{
    var all = await repo.GetAllAsync();
    if (!string.IsNullOrEmpty(status))
        all = all.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    return Results.Ok(all);
})
.WithName("GetProcesses")
.WithTags("Processes");

// ── GET /processes/{id} ─────────────────────────────────────
app.MapGet("/processes/{id:int}", async (int id, IProcessRepository repo) =>
    await repo.GetByIdAsync(id) is BusinessProcess p
        ? Results.Ok(p)
        : Results.NotFound(new { message = $"Process {id} not found" }))
.WithName("GetProcess")
.WithTags("Processes");

// ── POST /processes ─────────────────────────────────────────
app.MapPost("/processes", async (BusinessProcess process, IProcessRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(process.Name))
        return Results.BadRequest(new { message = "Process name is required" });

    var created = await repo.CreateAsync(process);
    return Results.Created($"/processes/{created.Id}", created);
})
.WithName("CreateProcess")
.WithTags("Processes");

// ── PUT /processes/{id} ─────────────────────────────────────
var validStatuses = new[] { "PENDING", "IN_PROGRESS", "COMPLETED", "FAILED" };

app.MapPut("/processes/{id:int}", async (int id, BusinessProcess updated, IProcessRepository repo) =>
{
    if (!validStatuses.Contains(updated.Status))
        return Results.BadRequest(new { message = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}" });

    return await repo.UpdateAsync(id, updated) is BusinessProcess p
        ? Results.Ok(p)
        : Results.NotFound(new { message = $"Process {id} not found" });
})
.WithName("UpdateProcess")
.WithTags("Processes");

// ── DELETE /processes/{id} ──────────────────────────────────
app.MapDelete("/processes/{id:int}", async (int id, IProcessRepository repo) =>
    await repo.DeleteAsync(id)
        ? Results.NoContent()
        : Results.NotFound(new { message = $"Process {id} not found" }))
.WithName("DeleteProcess")
.WithTags("Processes");

// ── GET /processes/{id}/audit ───────────────────────────────
// I return the full audit trail for a single process
app.MapGet("/processes/{id:int}/audit", async (int id, AppDbContext db) =>
{
    var logs = await db.AuditLogs
        .Where(l => l.ProcessId == id)
        .OrderByDescending(l => l.ChangedAt)
        .ToListAsync();

    return logs.Any()
        ? Results.Ok(logs)
        : Results.NotFound(new { message = $"No audit logs found for process {id}" });
})
.WithName("GetAuditLog")
.WithTags("Audit");

// ── GET /audit ──────────────────────────────────────────────
// I return all audit logs across all processes
app.MapGet("/audit", async (AppDbContext db) =>
    Results.Ok(await db.AuditLogs.OrderByDescending(l => l.ChangedAt).ToListAsync()))
.WithName("GetAllAuditLogs")
.WithTags("Audit");

// ── POST /processes/export ──────────────────────────────────
// I export all processes to JSON — uploads to S3 if configured
app.MapPost("/processes/export", async (IProcessRepository repo, AwsS3Service s3) =>
{
    var processes = await repo.GetAllAsync();
    var result    = await s3.ExportProcessesAsync(processes);

    return Results.Ok(new
    {
        message    = "Export successful",
        count      = processes.Count,
        url        = result.Url ?? "S3 not configured — see json field",
        json       = result.Url == null ? result.Json : null
    });
})
.WithName("ExportProcesses")
.WithTags("Export");

// ── GET /stats ──────────────────────────────────────────────
// I return a live summary of all process counts by status
app.MapGet("/stats", async (AppDbContext db) =>
{
    var all = await db.Processes.ToListAsync();
    return Results.Ok(new
    {
        total      = all.Count,
        pending    = all.Count(p => p.Status == "PENDING"),
        inProgress = all.Count(p => p.Status == "IN_PROGRESS"),
        completed  = all.Count(p => p.Status == "COMPLETED"),
        failed     = all.Count(p => p.Status == "FAILED"),
        asAt       = DateTime.UtcNow
    });
})
.WithName("GetStats")
.WithTags("System");

// I serve the dashboard UI from wwwroot
app.MapFallbackToFile("index.html");

app.Run();

// I expose Program so the test project can reference it
public partial class Program { }
