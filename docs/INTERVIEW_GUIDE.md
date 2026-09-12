# ClaimsOps interview guide

## 30-second version

ClaimsOps is a small insurance-workflow system written in C#/.NET. Claims move through a guarded lifecycle, are ranked by a transparent SLA-risk score, persist to SQLite, and keep an audit trail. The part I would emphasize is explainability: the API can tell an adjuster which factor is pushing a claim up the queue and give a simple counterfactual about how time changes urgency.

## Five-minute walkthrough

1. **Start with the business problem.** A claims team cannot work every case at once, so software has to decide what surfaces first. A bad queue can miss urgent work.
2. **Show `Claim.cs`.** It is an immutable record and owns the transparent risk calculation.
3. **Show `ClaimWorkflow.cs`.** Status changes are a state machine, not arbitrary strings.
4. **Show `RiskExplainer.cs`.** It decomposes the score into age/severity/exposure contributions and creates a counterfactual explanation.
5. **Show `Program.cs`.** Minimal API endpoints use the domain layer rather than reimplementing rules in controllers.
6. **Show `SqliteClaimStore.cs`.** Persistence and audit writes happen transactionally.
7. **Show integration tests.** They boot the real API with a temporary database and call HTTP endpoints.

## Questions I should be able to answer

### Why is age weighted most heavily?
It is a product choice for the prototype, not a universal insurance rule. I wanted SLA aging to have visible impact while severity and exposure still matter. In a real system those weights would be versioned business configuration, validated against operational outcomes, and approved by domain owners.

### Why not sort by severity only?
Severity alone ignores a medium claim that has been waiting for a long time and a high-exposure claim that needs attention. A composite score makes the tradeoff explicit.

### Is the score a machine-learning model?
No. That is intentional. It is a deterministic prioritization policy. The project demonstrates that sometimes a transparent business rule is more appropriate than ML, especially when operators need to understand and challenge a ranking.

### How would you prevent two adjusters from overwriting each other?
Add a version column or row-version token and require optimistic-concurrency checks on updates. A stale request would return `409 Conflict` instead of silently overwriting newer state.

### Why SQLite instead of PostgreSQL?
SQLite keeps the repo zero-setup and still demonstrates real persistence, SQL, and transactions. The repository boundary is where I would swap in PostgreSQL or SQL Server for production.

### What would you secure first?
Authentication, role-based authorization, PII protection, stricter validation, secret management, and an audit store that application users cannot rewrite.

## One thing that makes the project memorable

The endpoint I would demo is `/api/claims/{id}/explain`, not just CRUD. The point is: **if software changes the order in which people receive attention, the system should be able to explain that order.**

## What I would not claim

- This is not a real insurer's scoring policy.
- The synthetic claims do not represent real customers.
- The score has not been validated for production claim outcomes.
- SQLite is a portfolio-friendly persistence choice, not my recommendation for a large claims platform.
