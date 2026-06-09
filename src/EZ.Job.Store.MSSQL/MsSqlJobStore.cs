using System.Text.Json;
using EZ.Job.Core;
using Microsoft.Data.SqlClient;

namespace EZJob.Store.MSSQL;

public sealed class MsSqlJobStore : IJobStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _connectionString;

    public MsSqlJobStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTableAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ez_jobs' AND xtype='U')
            CREATE TABLE ez_jobs (
                id              NVARCHAR(36) PRIMARY KEY,
                type_name       NVARCHAR(MAX) NOT NULL,
                method_name     NVARCHAR(MAX) NOT NULL,
                argument_types  NVARCHAR(MAX) NOT NULL,
                arguments       NVARCHAR(MAX) NOT NULL,
                status          TINYINT NOT NULL DEFAULT 0,
                created_at      DATETIME2(6) NOT NULL,
                error           NVARCHAR(MAX),
                recurring_job_id NVARCHAR(64),
                started_at      DATETIME2(6),
                completed_at    DATETIME2(6)
            )
            """, conn);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async ValueTask AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("""
            INSERT INTO ez_jobs (id, type_name, method_name, argument_types, arguments, status, created_at, error, recurring_job_id, started_at, completed_at)
            VALUES (@id, @type_name, @method_name, @argument_types, @arguments, @status, @created_at, @error, @recurring_job_id, @started_at, @completed_at)
            """, conn);

        cmd.Parameters.AddWithValue("@id", job.Id);
        cmd.Parameters.AddWithValue("@type_name", job.TypeName);
        cmd.Parameters.AddWithValue("@method_name", job.MethodName);
        cmd.Parameters.AddWithValue("@argument_types", JsonSerializer.Serialize(job.ArgumentTypes, JsonOptions));
        cmd.Parameters.AddWithValue("@arguments", SerializeArgs(job.Arguments));
        cmd.Parameters.AddWithValue("@status", (int)job.Status);
        cmd.Parameters.AddWithValue("@created_at", job.CreatedAt);
        cmd.Parameters.AddWithValue("@error", (object?)job.Error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@recurring_job_id", (object?)job.RecurringJobId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@started_at", (object?)job.StartedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@completed_at", (object?)job.CompletedAt ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Job?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("SELECT * FROM ez_jobs WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return ReadJob(reader);
        }

        return null;
    }

    public async ValueTask<IEnumerable<Job>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = new List<Job>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("SELECT * FROM ez_jobs ORDER BY created_at", conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            jobs.Add(ReadJob(reader));
        }

        return jobs;
    }

    public async ValueTask UpdateStatusAsync(string id, JobStatus status, string? error = null, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("""
            UPDATE ez_jobs
            SET status = @status,
                error = @error,
                started_at = CASE WHEN @status = 1 THEN COALESCE(started_at, @now) ELSE started_at END,
                completed_at = CASE WHEN @status IN (2, 3) THEN @now ELSE NULL END
            WHERE id = @id
            """, conn);

        cmd.Parameters.AddWithValue("@status", (int)status);
        cmd.Parameters.AddWithValue("@error", (object?)error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IEnumerable<Job>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var jobs = new List<Job>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("SELECT * FROM ez_jobs WHERE status = 0 ORDER BY created_at", conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            jobs.Add(ReadJob(reader));
        }

        return jobs;
    }

    private static Job ReadJob(SqlDataReader reader)
    {
        return new Job(
            Id: reader.GetString(0),
            TypeName: reader.GetString(1),
            MethodName: reader.GetString(2),
            ArgumentTypes: JsonSerializer.Deserialize<string[]>(reader.GetString(3), JsonOptions) ?? [],
            Arguments: DeserializeArgs(reader.GetString(4)),
            Status: (JobStatus)reader.GetByte(5),
            CreatedAt: reader.GetDateTime(6),
            Error: reader.IsDBNull(7) ? null : reader.GetString(7),
            StartedAt: reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            CompletedAt: reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            RecurringJobId: reader.IsDBNull(8) ? null : reader.GetString(8));
    }

    private static string SerializeArgs(object?[] args)
    {
        return JsonSerializer.Serialize(args, JsonOptions);
    }

    private static object?[] DeserializeArgs(string json)
    {
        return JsonSerializer.Deserialize<object?[]>(json, JsonOptions) ?? [];
    }
}
