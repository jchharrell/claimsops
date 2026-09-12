namespace ClaimsOps.Core;

public enum ClaimStatus { New, Investigating, PendingDocuments, Approved, Closed }
public enum Severity { Low = 1, Medium = 2, High = 3, Critical = 4 }

public sealed record Claim(
    string Id,
    string PolicyNumber,
    Severity Severity,
    decimal Exposure,
    DateTimeOffset ReportedAt,
    ClaimStatus Status = ClaimStatus.New,
    string AssignedAdjuster = "Unassigned")
{
    public int AgeHours(DateTimeOffset now) => Math.Max(0, (int)(now - ReportedAt).TotalHours);

    public double SlaRisk(DateTimeOffset now)
    {
        var age = Math.Min(AgeHours(now) / 72.0, 1.5);
        var severity = (int)Severity / 4.0;
        var exposure = Math.Min((double)Exposure / 100_000.0, 1.0);
        return Math.Round(Math.Min(1.0, 0.45 * age + 0.35 * severity + 0.20 * exposure), 3);
    }
}
