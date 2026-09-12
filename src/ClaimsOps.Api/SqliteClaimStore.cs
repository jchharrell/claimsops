using ClaimsOps.Core;
using Microsoft.Data.Sqlite;

namespace ClaimsOps.Api;

public sealed record AuditEvent(long Id, string ClaimId, string EventType, string Detail, DateTimeOffset CreatedAt);

public sealed class SqliteClaimStore
{
    private readonly string _connectionString;

    public SqliteClaimStore(IConfiguration configuration)
    {
        var path = configuration["CLAIMSOPS_DB"] ?? "claimsops.db";
        _connectionString = $"Data Source={path}";
        Initialize();
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
        CREATE TABLE IF NOT EXISTS claims (
          id TEXT PRIMARY KEY,
          policy_number TEXT NOT NULL,
          severity INTEGER NOT NULL,
          exposure REAL NOT NULL,
          reported_at TEXT NOT NULL,
          status INTEGER NOT NULL,
          assigned_adjuster TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS audit_events (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          claim_id TEXT NOT NULL,
          event_type TEXT NOT NULL,
          detail TEXT NOT NULL,
          created_at TEXT NOT NULL
        );
        """;
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<Claim> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, policy_number, severity, exposure, reported_at, status, assigned_adjuster FROM claims";
        using var reader = command.ExecuteReader();
        var items = new List<Claim>();
        while (reader.Read())
        {
            items.Add(new Claim(
                reader.GetString(0), reader.GetString(1), (Severity)reader.GetInt32(2),
                reader.GetDecimal(3), DateTimeOffset.Parse(reader.GetString(4)),
                (ClaimStatus)reader.GetInt32(5), reader.GetString(6)));
        }
        return items;
    }

    public Claim? Get(string id) => GetAll().FirstOrDefault(c => c.Id == id);

    public void Upsert(Claim claim, string reason = "claim saved")
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
        INSERT INTO claims(id, policy_number, severity, exposure, reported_at, status, assigned_adjuster)
        VALUES($id,$policy,$severity,$exposure,$reported,$status,$adjuster)
        ON CONFLICT(id) DO UPDATE SET
          policy_number=excluded.policy_number,
          severity=excluded.severity,
          exposure=excluded.exposure,
          reported_at=excluded.reported_at,
          status=excluded.status,
          assigned_adjuster=excluded.assigned_adjuster;
        """;
        command.Parameters.AddWithValue("$id", claim.Id);
        command.Parameters.AddWithValue("$policy", claim.PolicyNumber);
        command.Parameters.AddWithValue("$severity", (int)claim.Severity);
        command.Parameters.AddWithValue("$exposure", claim.Exposure);
        command.Parameters.AddWithValue("$reported", claim.ReportedAt.ToString("O"));
        command.Parameters.AddWithValue("$status", (int)claim.Status);
        command.Parameters.AddWithValue("$adjuster", claim.AssignedAdjuster);
        command.ExecuteNonQuery();
        WriteAudit(connection, transaction, claim.Id, "saved", reason);
        transaction.Commit();
    }

    public void RecordTransition(Claim before, Claim after)
    {
        Upsert(after, $"status {before.Status} -> {after.Status}");
    }

    public IReadOnlyList<AuditEvent> Audit(string claimId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, claim_id, event_type, detail, created_at FROM audit_events WHERE claim_id=$id ORDER BY id";
        command.Parameters.AddWithValue("$id", claimId);
        using var reader = command.ExecuteReader();
        var events = new List<AuditEvent>();
        while (reader.Read())
            events.Add(new AuditEvent(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), DateTimeOffset.Parse(reader.GetString(4))));
        return events;
    }

    private static void WriteAudit(SqliteConnection connection, SqliteTransaction tx, string claimId, string eventType, string detail)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "INSERT INTO audit_events(claim_id,event_type,detail,created_at) VALUES($id,$type,$detail,$at)";
        command.Parameters.AddWithValue("$id", claimId);
        command.Parameters.AddWithValue("$type", eventType);
        command.Parameters.AddWithValue("$detail", detail);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }
}
