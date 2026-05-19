using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ProcessTracker;

// I create a test class that tests all my API endpoints
// xUnit automatically discovers and runs all methods marked with [Fact]
public class ProcessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    // I set up an in-memory test database so my real database is never touched
    public ProcessTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // I remove the real SQLite database connection
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // I replace it with an in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    // I test that creating a process returns 201 Created
    [Fact]
    public async Task CreateProcess_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/processes", new
        {
            name = "Test Process",
            assignedTo = "Phetho Tlaka" 
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // I test that the created process has PENDING status by default
    [Fact]
    public async Task CreateProcess_DefaultStatusIsPending()
    {
        var response = await _client.PostAsJsonAsync("/processes", new
        {
            name = "Test Process",
            assignedTo = "Phetho Tlaka"
        });
        var process = await response.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.Equal("PENDING", process!.Status);
    }

    // I test that getting all processes returns 200 OK
    [Fact]
    public async Task GetProcesses_Returns200()
    {
        var response = await _client.GetAsync("/processes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // I test that getting a non-existent process returns 404
    [Fact]
    public async Task GetProcess_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/processes/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // I test that updating a process status works correctly
    [Fact]
    public async Task UpdateProcess_StatusChanges()
    {
        var create = await _client.PostAsJsonAsync("/processes", new
        {
            name = "Update Test",
            assignedTo = "Phetho Tlaka"
        });
        var created = await create.Content.ReadFromJsonAsync<BusinessProcess>();

        var response = await _client.PutAsJsonAsync($"/processes/{created!.Id}", new
        {
            name = "Update Test",
            status = "COMPLETED",
            assignedTo = "Phetho Tlaka"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<BusinessProcess>();
        Assert.Equal("COMPLETED", updated!.Status);
    }

    // I test that deleting a process returns 204 No Content
    [Fact]
    public async Task DeleteProcess_Returns204()
    {
        var create = await _client.PostAsJsonAsync("/processes", new
        {
            name = "Delete Test",
            assignedTo = "Phetho Tlaka"
        });
        var created = await create.Content.ReadFromJsonAsync<BusinessProcess>();

        var response = await _client.DeleteAsync($"/processes/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}