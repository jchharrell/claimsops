using ClaimsOps.Core;
using Xunit;

namespace ClaimsOps.Core.Tests;

public class RiskExplainerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Explanation_uses_same_score_as_claim()
    {
        var claim = new Claim("CLM-1", "POL-1", Severity.High, 75_000, Now.AddHours(-50));
        var result = new RiskExplainer().Explain(claim, Now);
        Assert.Equal(claim.SlaRisk(Now), result.Score);
        Assert.Equal(3, result.Factors.Count);
    }

    [Fact]
    public void Old_claim_gets_age_counterfactual()
    {
        var claim = new Claim("CLM-2", "POL-2", Severity.Low, 5_000, Now.AddHours(-60));
        var result = new RiskExplainer().Explain(claim, Now);
        Assert.Contains("24 hours earlier", result.Counterfactual);
    }
}
