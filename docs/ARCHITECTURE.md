# ClaimsOps Architecture

ClaimsOps is a small C#/.NET claims-triage system designed around explicit business rules, explainable prioritization, and auditable state changes.

## Design goals

- keep domain rules independent from transport and persistence concerns
- prevent invalid claim lifecycle transitions
- make prioritization explainable to an operator
- persist claim updates and audit events transactionally
- keep the project runnable with minimal local setup

## Components

### Domain layer

`src/ClaimsOps.Core/Claim.cs` defines the immutable claim model and transparent SLA-risk calculation.

`src/ClaimsOps.Core/ClaimWorkflow.cs` owns lifecycle transitions and prioritization rules. The workflow is modeled as an explicit state machine so status changes cannot bypass business rules.

`src/ClaimsOps.Core/RiskExplainer.cs` decomposes a claim's risk score into age, severity, and exposure contributions and produces a counterfactual explanation of how urgency changes over time.

### API layer

The ASP.NET Core Minimal API exposes claim retrieval, creation, transitions, prioritization, and risk explanations. HTTP handlers delegate to the domain layer rather than duplicating business logic.

### Persistence

SQLite provides zero-setup relational persistence for the portfolio project. Claim updates and audit entries are written together so a successful state change has a corresponding history record.

For a production deployment, the persistence boundary could be moved to PostgreSQL or SQL Server without changing the core workflow rules.

## Prioritization model

The SLA-risk score combines claim age, severity, and financial exposure. It is intentionally deterministic rather than machine-learned. The goal is to make a queueing decision inspectable and challengeable by an operator.

The weights are prototype policy choices, not insurance-industry recommendations. In a production system, weights and thresholds would be versioned configuration governed by domain owners and validated against operational outcomes.

## Concurrency and production concerns

A production version would add optimistic concurrency, authentication and role-based authorization, stronger request validation, secrets management, protected audit storage, structured telemetry, and database migrations.

## Verification

The repository includes xUnit domain tests, HTTP integration tests that boot the real API against a temporary database, and a GitHub Actions workflow that builds, tests, smoke-tests, and verifies the Docker image.

## Scope

All data is synthetic. ClaimsOps is a portfolio engineering project and does not implement a real insurer's scoring policy.