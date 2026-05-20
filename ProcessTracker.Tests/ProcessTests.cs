using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using ProcessTracker;
using Xunit;

namespace ProcessTracker.Tests;

public class ProcessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProcessTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // I generate the database name ONCE outside the lambda
    // This ensures every request in the same test sees the same database
    private HttpClient CreateClient()
    {
        var dbName = "TestDb_" + Guid.NewGuid();

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var bgDescriptors = services
                    .Where(d => d.ImplementationType == typeof(ProcessTimeoutService))
                    .ToList();
                foreach (var d in bgDescriptors)
                    services.Remove(d);
            });
        }).CreateClient();
    }

    // ── TEST 1 ──────────────────────────────────────────────
    [Fact]
    public async Task CreateProcess_Returns201()
    {
        var client   = CreateClient();
        var process  = new BusinessProcess { Name = "Test Process", AssignedTo = "Phetho" };
        var response = await client.PostAsJsonAsync("/processes", process);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── TEST 2 ──────────────────────────────────────────────
    [Fact]
    public async Task CreateProcess_DefaultStatusIsPending()
    {
        var client   = CreateClient();
        var process  = new BusinessProcess { Name = "Status Test", AssignedTo = "Phetho" };
        var response = await client.PostAsJsonAsync("/processes", process);
        var created  = await response.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.Equal("PENDING", created?.Status);
    }

    // ── TEST 3 ──────────────────────────────────────────────
    [Fact]
    public async Task GetProcesses_Returns200()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/processes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TEST 4 ──────────────────────────────────────────────
    [Fact]
    public async Task GetProcess_NotFound_Returns404()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/processes/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── TEST 5 ──────────────────────────────────────────────
    [Fact]
    public async Task UpdateProcess_StatusChanges()
    {
        var client  = CreateClient();

        var created = await client.PostAsJsonAsync("/processes",
            new BusinessProcess { Name = "Update Test", AssignedTo = "Phetho" });
        var process = await created.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.NotNull(process);

        process.Status = "IN_PROGRESS";
        var updated = await client.PutAsJsonAsync($"/processes/{process.Id}", process);
        var result  = await updated.Content.ReadFromJsonAsync<BusinessProcess>();

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("IN_PROGRESS", result?.Status);
    }

    // ── TEST 6 ──────────────────────────────────────────────
    [Fact]
    public async Task DeleteProcess_Returns204()
    {
        var client  = CreateClient();

        var created  = await client.PostAsJsonAsync("/processes",
            new BusinessProcess { Name = "Delete Test", AssignedTo = "Phetho" });
        var process  = await created.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.NotNull(process);

        var response = await client.DeleteAsync($"/processes/{process.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── TEST 7 ──────────────────────────────────────────────
    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TEST 8 ──────────────────────────────────────────────
    [Fact]
    public async Task CreateProcess_EmptyName_Returns400()
    {
        var client   = CreateClient();
        var process  = new BusinessProcess { Name = "", AssignedTo = "Phetho" };
        var response = await client.PostAsJsonAsync("/processes", process);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── TEST 9 ──────────────────────────────────────────────
    [Fact]
    public async Task GetStats_Returns200()
    {
        var client   = CreateClient();
        var response = await client.GetAsync("/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TEST 10 ─────────────────────────────────────────────
    [Fact]
    public async Task GetAuditLog_AfterUpdate_ReturnsLogs()
    {
        var client  = CreateClient();

        var created = await client.PostAsJsonAsync("/processes",
            new BusinessProcess { Name = "Audit Test", AssignedTo = "Phetho" });
        var process = await created.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.NotNull(process);

        process.Status = "COMPLETED";
        await client.PutAsJsonAsync($"/processes/{process.Id}", process);

        var auditResponse = await client.GetAsync($"/processes/{process.Id}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
    }

    // ── TEST 11 ─────────────────────────────────────────────
    [Fact]
    public async Task UpdateProcess_InvalidStatus_Returns400()
    {
        var client  = CreateClient();

        var created = await client.PostAsJsonAsync("/processes",
            new BusinessProcess { Name = "Invalid Status Test", AssignedTo = "Phetho" });
        var process = await created.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.NotNull(process);

        process.Status = "INVALID_STATUS";
        var response = await client.PutAsJsonAsync($"/processes/{process.Id}", process);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── TEST 12 ─────────────────────────────────────────────
    [Fact]
    public async Task ExportProcesses_Returns200()
    {
        var client   = CreateClient();
        var response = await client.PostAsync("/processes/export", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}