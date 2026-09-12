# ClaimsOps

**Explainable insurance-claims triage with a real ASP.NET Core API, SQLite persistence, lifecycle rules, audit history, and counterfactual risk explanations.**

ClaimsOps is built around a question I think matters in enterprise software: if a system changes the order in which humans do important work, can it explain *why*? The project ranks synthetic claims by SLA risk, but every score is decomposable into age, severity, and exposure contributions. A recruiter can run the API, create a claim, move it through a guarded workflow, ask why it is urgent, and inspect the audit trail.

> Portfolio project using synthetic data only. It is not affiliated with an insurer and contains no real customer data.

## Why this is more than a CRUD demo

- **Domain state machine** prevents impossible transitions such as `New -> Closed`.
- **Explainable triage** ranks work by a transparent SLA-risk score rather than a black-box prediction.
- **Counterfactual explanation** tells an adjuster what is causing urgency and how time is changing the queue position.
- **Persistent audit history** records claim creation and workflow movement in SQLite transactions.
- **REST API + Swagger** makes the business rules usable from any client.
- **Zero external infrastructure**: `dotnet run` creates the SQLite database automatically.
- **Docker support** gives the project a one-command production-like runtime.
- **Unit + integration tests** validate the domain layer and exercise the HTTP API end-to-end in CI.

## Architecture

```text
browser prototype (index.html / app.js)

                    HTTP
                     |
              ClaimsOps.Api
        Minimal API + validation
             /             \
   ClaimWorkflow       SqliteClaimStore
   RiskExplainer       claims + audit_events
             \             /
               ClaimsOps.Core
      immutable domain records + rules
```

The important boundary is `ClaimsOps.Core`: it has no web or database dependency. That means the lifecycle and prioritization rules can be tested without ASP.NET or SQLite and could be reused by a worker, API, or message consumer.

## Quick start

Requires .NET 8.

```bash
git clone https://github.com/jchharrell/claimsops.git
cd claimsops
dotnet run --project src/ClaimsOps.Api/ClaimsOps.Api.csproj
```

Then open Swagger at:

```text
http://localhost:5000/swagger
```

If your local ASP.NET port differs, use the URL printed by `dotnet run`.

A ready-to-run request sequence is in `examples/demo.http`.

### Docker

```bash
docker build -t claimsops .
docker run --rm -p 8080:8080 -v claimsops-data:/data claimsops
```

Swagger will be available at `http://localhost:8080/swagger`.

## Useful endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /health` | runtime smoke check |
| `GET /api/claims` | ranked claims queue with current risk |
| `POST /api/claims` | validated claim creation |
| `GET /api/claims/{id}` | claim + explanation + audit history |
| `GET /api/claims/{id}/explain` | risk decomposition and counterfactual |
| `POST /api/claims/{id}/transition` | guarded lifecycle transition |
| `GET /api/claims/{id}/audit` | append-style operational history |

## The explainability idea

`Claim.SlaRisk()` intentionally uses a small transparent scoring model:

```text
risk = 0.45 * age_component
     + 0.35 * severity_component
     + 0.20 * exposure_component
```

`RiskExplainer` calculates the same components and reports the largest contributor. It also gives a counterfactual such as: *if this claim had been reviewed 24 hours earlier, its queue risk would be lower.*

That creates an interview discussion beyond “I built an API”: prioritization systems should be auditable because a ranking can change who gets attention first.

## Tests

```bash
dotnet test tests/ClaimsOps.Core.Tests/ClaimsOps.Core.Tests.csproj
dotnet test tests/ClaimsOps.Api.Tests/ClaimsOps.Api.Tests.csproj
```

The API tests boot the application in-memory, create an isolated temporary SQLite database, call real HTTP endpoints, and verify ranking/explanation/audit behavior.

GitHub Actions runs both suites and builds the API on every push and pull request.

## Design decisions I can explain

**Why SQLite?** It makes the project genuinely runnable with no database setup while still demonstrating SQL persistence and transactions. In a production deployment I would move the repository behind an interface and use PostgreSQL or SQL Server.

**Why not use ML for claim priority?** The project is about application delivery and accountable workflow. A transparent scoring model lets the operator challenge the ranking and makes the business rules obvious in code.

**Why immutable records?** Workflow transitions return a new `Claim`, which makes state changes easier to reason about and test.

**Why keep the Core project framework-free?** Business rules should not become impossible to test because they are coupled to a database or HTTP context.

## If I were shipping this at an insurer

Next production work would include authentication/authorization, optimistic concurrency, PII encryption, append-only audit storage, role-specific views, OpenTelemetry traces/metrics, migrations, PostgreSQL/SQL Server, idempotency keys, and event publication for downstream claim systems.

## Interview walkthrough

See [`docs/INTERVIEW_GUIDE.md`](docs/INTERVIEW_GUIDE.md) for a five-minute explanation, likely technical questions, and the tradeoffs I would discuss.
