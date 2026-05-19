using Microsoft.EntityFrameworkCore;
using ProcessTracker;

// I create the web application builder — entry point of my API
var builder = WebApplication.CreateBuilder(args);

// I add Swagger so I can test all endpoints in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// I add CORS so my frontend UI can call the API from the browser
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// I configure SQLite as my database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=ProcessTracker.db"));

// I register my repository — anywhere IProcessRepository is needed,
// .NET injects ProcessRepository automatically
builder.Services.AddScoped<IProcessRepository, ProcessRepository>();

var app = builder.Build();

// I create the database automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// I enable Swagger UI for testing
app.UseSwagger();
app.UseSwaggerUI();

// I enable CORS before routing
app.UseCors();

// ── GET /processes ─────────────────────────────────────
// I return all processes using the repository
app.MapGet("/processes", async (IProcessRepository repo) =>
    Results.Ok(await repo.GetAllAsync()));

// ── GET /processes/{id} ────────────────────────────────
// I find a specific process — 404 if not found
app.MapGet("/processes/{id}", async (int id, IProcessRepository repo) =>
    await repo.GetByIdAsync(id) is BusinessProcess p
        ? Results.Ok(p)
        : Results.NotFound());

// ── POST /processes ────────────────────────────────────
// I create a new process through the repository
app.MapPost("/processes", async (BusinessProcess p, IProcessRepository repo) =>
{
    var created = await repo.CreateAsync(p);
    return Results.Created($"/processes/{created.Id}", created);
});

// ── PUT /processes/{id} ────────────────────────────────
// I update an existing process through the repository
app.MapPut("/processes/{id}", async (int id, BusinessProcess updated, IProcessRepository repo) =>
    await repo.UpdateAsync(id, updated) is BusinessProcess p
        ? Results.Ok(p)
        : Results.NotFound());

// ── DELETE /processes/{id} ─────────────────────────────
// I delete a process through the repository
app.MapDelete("/processes/{id}", async (int id, IProcessRepository repo) =>
    await repo.DeleteAsync(id)
        ? Results.NoContent()
        : Results.NotFound());

// I serve static files so my UI can load from wwwroot/
app.UseStaticFiles();

// I redirect root to the UI
app.MapGet("/ui", async context =>
{
    context.Response.Redirect("/index.html");
});

app.Run();

// I expose Program class so the test project can reference it
public partial class Program { }