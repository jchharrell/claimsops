namespace ClaimsOps.Core;

public sealed record RiskFactor(string Name, double Contribution, string Why);
public sealed record RiskExplanation(double Score, IReadOnlyList<RiskFactor> Factors, string Summary, string Counterfactual);

/// <summary>
/// Produces a human-readable explanation for the same transparent score used by Claim.SlaRisk.
/// The point is not to pretend the score is an ML model; it is to make queue decisions auditable.
/// </summary>
public sealed class RiskExplainer
{
    public RiskExplanation Explain(Claim claim, DateTimeOffset now)
    {
        var ageRaw = Math.Min(claim.AgeHours(now) / 72.0, 1.5);
        var severityRaw = (int)claim.Severity / 4.0;
        var exposureRaw = Math.Min((double)claim.Exposure / 100_000.0, 1.0);

        var age = Math.Round(0.45 * ageRaw, 3);
        var severity = Math.Round(0.35 * severityRaw, 3);
        var exposure = Math.Round(0.20 * exposureRaw, 3);
        var score = claim.SlaRisk(now);

        var factors = new[]
        {
            new RiskFactor("age", age, $"Open for {claim.AgeHours(now)} hours"),
            new RiskFactor("severity", severity, $"Severity is {claim.Severity}"),
            new RiskFactor("exposure", exposure, $"Exposure is {claim.Exposure:C0}")
        }.OrderByDescending(f => f.Contribution).ToArray();

        var top = factors[0];
        var summary = $"{claim.Id} scores {score:0.000}; {top.Name} is the largest contributor ({top.Contribution:0.000}).";

        var counterfactual = claim.AgeHours(now) >= 48
            ? "If this claim had been reviewed 24 hours earlier, its queue risk would be lower; age is actively increasing urgency."
            : claim.Severity >= Severity.High
                ? "The claim remains high-priority even while young because severity is carrying a large share of risk."
                : "This claim is not currently dominated by a single urgent factor; exposure and aging will determine whether it rises in the queue.";

        return new RiskExplanation(score, factors, summary, counterfactual);
    }
}
