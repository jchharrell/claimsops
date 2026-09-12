namespace ClaimsOps.Core;

public sealed class ClaimWorkflow
{
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> Allowed =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.New] = [ClaimStatus.Investigating],
            [ClaimStatus.Investigating] = [ClaimStatus.PendingDocuments, ClaimStatus.Approved],
            [ClaimStatus.PendingDocuments] = [ClaimStatus.Investigating, ClaimStatus.Approved],
            [ClaimStatus.Approved] = [ClaimStatus.Closed],
            [ClaimStatus.Closed] = []
        };

    public Claim Transition(Claim claim, ClaimStatus next)
    {
        if (!Allowed[claim.Status].Contains(next))
            throw new InvalidOperationException($"Invalid transition: {claim.Status} -> {next}");
        return claim with { Status = next };
    }

    public IEnumerable<Claim> Prioritize(IEnumerable<Claim> claims, DateTimeOffset now) =>
        claims.OrderByDescending(c => c.SlaRisk(now))
              .ThenByDescending(c => c.Exposure)
              .ThenBy(c => c.ReportedAt);
}
