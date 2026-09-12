using ClaimsOps.Api;
using ClaimsOps.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<SqliteClaimStore>();
builder.Services.AddSingleton<ClaimWorkflow>();
builder.Services.AddSingleton<RiskExplainer>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "claimsops-api" }));

app.MapGet("/api/claims", (SqliteClaimStore store, ClaimWorkflow workflow) =>
{
    var now = DateTimeOffset.UtcNow;
    var ranked = workflow.Prioritize(store.GetAll(), now)
        .Select((claim, index) => new { rank = index + 1, claim, risk = claim.SlaRisk(now) });
    return Results.Ok(ranked);
});

app.MapGet("/api/claims/{id}", (string id, SqliteClaimStore store, RiskExplainer explainer) =>
{
    var claim = store.Get(id);
    return claim is null
        ? Results.NotFound()
        : Results.Ok(new { claim, explanation = explainer.Explain(claim, DateTimeOffset.UtcNow), audit = store.Audit(id) });
});

app.MapPost("/api/claims", (CreateClaim request, SqliteClaimStore store) =>
{
    var errors = request.Validate();
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    if (store.Get(request.Id) is not null) return Results.Conflict(new { error = "claim id already exists" });

    var claim = new Claim(request.Id.Trim(), request.PolicyNumber.Trim(), request.Severity, request.Exposure,
        request.ReportedAt ?? DateTimeOffset.UtcNow, ClaimStatus.New, request.AssignedAdjuster?.Trim() ?? "Unassigned");
    store.Upsert(claim, "claim created through API");
    return Results.Created($"/api/claims/{claim.Id}", claim);
});

app.MapPost("/api/claims/{id}/transition", (string id, TransitionClaim request, SqliteClaimStore store, ClaimWorkflow workflow) =>
{
    var claim = store.Get(id);
    if (claim is null) return Results.NotFound();
    try
    {
        var updated = workflow.Transition(claim, request.Status);
        store.RecordTransition(claim, updated);
        return Results.Ok(updated);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/claims/{id}/explain", (string id, SqliteClaimStore store, RiskExplainer explainer) =>
{
    var claim = store.Get(id);
    return claim is null ? Results.NotFound() : Results.Ok(explainer.Explain(claim, DateTimeOffset.UtcNow));
});

app.MapGet("/api/claims/{id}/audit", (string id, SqliteClaimStore store) =>
    store.Get(id) is null ? Results.NotFound() : Results.Ok(store.Audit(id)));

app.Run();

public sealed record CreateClaim(string Id, string PolicyNumber, Severity Severity, decimal Exposure, DateTimeOffset? ReportedAt, string? AssignedAdjuster)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Id)) errors["id"] = ["Id is required"];
        if (string.IsNullOrWhiteSpace(PolicyNumber)) errors["policyNumber"] = ["Policy number is required"];
        if (Exposure < 0 || Exposure > 10_000_000) errors["exposure"] = ["Exposure must be between 0 and 10,000,000"];
        if (!Enum.IsDefined(Severity)) errors["severity"] = ["Severity is invalid"];
        return errors;
    }
}

public sealed record TransitionClaim(ClaimStatus Status);

public partial class Program { }
