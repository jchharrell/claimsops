using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ClaimsOps.Api.Tests;

public sealed class ClaimsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"claimsops-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["CLAIMSOPS_DB"] = _db });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_db)) File.Delete(_db);
    }
}

public class ClaimsApiTests : IClassFixture<ClaimsApiFactory>
{
    private readonly HttpClient _client;

    public ClaimsApiTests(ClaimsApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_is_runnable()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Claim_can_be_created_ranked_explained_and_audited()
    {
        var create = new
        {
            id = "CLM-9001",
            policyNumber = "POL-44",
            severity = 4,
            exposure = 85000,
            reportedAt = DateTimeOffset.UtcNow.AddHours(-30),
            assignedAdjuster = "Maya"
        };

        var created = await _client.PostAsJsonAsync("/api/claims", create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var queue = await _client.GetAsync("/api/claims");
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        Assert.Contains("CLM-9001", await queue.Content.ReadAsStringAsync());

        var explain = await _client.GetAsync("/api/claims/CLM-9001/explain");
        var explanationBody = await explain.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, explain.StatusCode);
        Assert.Contains("counterfactual", explanationBody, StringComparison.OrdinalIgnoreCase);

        var audit = await _client.GetAsync("/api/claims/CLM-9001/audit");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        Assert.Contains("claim created through API", await audit.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_transition_returns_bad_request()
    {
        await _client.PostAsJsonAsync("/api/claims", new
        {
            id = "CLM-9002", policyNumber = "POL-45", severity = 2, exposure = 12000,
            reportedAt = DateTimeOffset.UtcNow, assignedAdjuster = "Noah"
        });

        var response = await _client.PostAsJsonAsync("/api/claims/CLM-9002/transition", new { status = 4 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
