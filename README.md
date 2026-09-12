# ClaimsOps

A claims-triage and workflow project built to practice the kind of stateful application-delivery problems found in insurance systems. The browser UI is a fast demo; the `src/` folder contains the C#/.NET domain layer that models the business rules.

## What it demonstrates

- explicit claim lifecycle state machine instead of arbitrary status mutation
- explainable SLA-risk scoring from age, severity, and exposure
- deterministic triage ordering with tie breakers
- immutable C# records and testable domain services
- xUnit tests and GitHub Actions CI
- client-side demo with filtering and persistent state

## Architecture

`index.html` / `app.js` → interactive demo

`ClaimsOps.Core/Claim.cs` → claim model + risk calculation

`ClaimsOps.Core/ClaimWorkflow.cs` → allowed transitions + prioritization

`ClaimsOps.Core.Tests/` → behavior tests

The domain layer intentionally has no database or web-framework dependency. That keeps the rules independently testable and makes it straightforward to place an ASP.NET API and PostgreSQL repository around it later.

## Run

Demo: open `index.html`.

Core tests (requires .NET 8):

```bash
dotnet test tests/ClaimsOps.Core.Tests/ClaimsOps.Core.Tests.csproj
```

## Design decisions I can explain

I used a state machine because a real claim should not jump from `New` directly to `Closed`. Risk scoring is deliberately transparent rather than ML-based: an adjuster should be able to understand why a claim moved up the queue. The score is capped and normalized so very large exposures cannot completely dominate age and severity.

## Next production steps

ASP.NET Minimal API, PostgreSQL persistence, optimistic concurrency, authentication/roles, append-only audit history, request validation, integration tests, and OpenTelemetry metrics.

## Scope

Portfolio prototype using synthetic data only. It does not represent a production insurer or real customer information.
