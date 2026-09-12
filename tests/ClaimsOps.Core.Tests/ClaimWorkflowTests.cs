using ClaimsOps.Core;

namespace ClaimsOps.Core.Tests;

public class ClaimWorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Risk_increases_for_older_high_severity_claims()
    {
        var low = new Claim("C1", "P1", Severity.Low, 2_000, Now.AddHours(-2));
        var urgent = new Claim("C2", "P2", Severity.Critical, 90_000, Now.AddHours(-60));
        Assert.True(urgent.SlaRisk(Now) > low.SlaRisk(Now));
    }

    [Fact]
    public void Workflow_rejects_skipping_directly_to_closed()
    {
        var claim = new Claim("C1", "P1", Severity.Medium, 5_000, Now);
        Assert.Throws<InvalidOperationException>(() => new ClaimWorkflow().Transition(claim, ClaimStatus.Closed));
    }

    [Fact]
    public void Prioritize_places_highest_risk_first()
    {
        var claims = new[] {
            new Claim("C1", "P1", Severity.Low, 1_000, Now.AddHours(-1)),
            new Claim("C2", "P2", Severity.Critical, 100_000, Now.AddHours(-70))
        };
        Assert.Equal("C2", new ClaimWorkflow().Prioritize(claims, Now).First().Id);
    }
}
