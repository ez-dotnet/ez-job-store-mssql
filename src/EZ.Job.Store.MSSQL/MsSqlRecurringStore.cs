using System.Text.Json;
using EZ.Job.Core;
using Microsoft.Data.SqlClient;

namespace EZJob.Store.MSSQL;

public sealed class MsSqlRecurringStore : IRecurringStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _connectionString;

    public MsSqlRecurringStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTableAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync().ConfigureAwait(false);

        await using var cmd = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ez_recurring_definitions' AND xtype='U')
            CREATE TABLE ez_recurring_definitions (
                id                  NVARCHAR(36) PRIMARY KEY,
                type_name           NVARCHAR(MAX) NOT NULL,
                method_name         NVARCHAR(MAX) NOT NULL,
                argument_types      NVARCHAR(MAX) NOT NULL,
                arguments           NVARCHAR(MAX) NOT NULL,
                cron_expression     NVARCHAR(MAX) NOT NULL,
                is_active           BIT NOT NULL DEFAULT 1,
                created_at_utc      DATETIME2(6) NOT NULL,
                last_execution_utc  DATETIME2(6)
            )
            """, conn);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async ValueTask AddOrUpdateAsync(RecurringDefinition definition, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("""
            IF EXISTS (SELECT 1 FROM ez_recurring_definitions WHERE id = @id)
                UPDATE ez_recurring_definitions SET
                    type_name = @type_name,
                    method_name = @method_name,
                    argument_types = @argument_types,
                    arguments = @arguments,
                    cron_expression = @cron_expression,
                    is_active = @is_active,
                    last_execution_utc = @last_execution_utc
                WHERE id = @id
            ELSE
                INSERT INTO ez_recurring_definitions (id, type_name, method_name, argument_types, arguments, cron_expression, is_active, created_at_utc, last_execution_utc)
                VALUES (@id, @type_name, @method_name, @argument_types, @arguments, @cron_expression, @is_active, @created_at_utc, @last_execution_utc)
            """, conn);

        AddParameters(cmd, definition);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("DELETE FROM ez_recurring_definitions WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id.ToString());

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<RecurringDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("SELECT * FROM ez_recurring_definitions WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", id.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return ReadDefinition(reader);
        }

        return null;
    }

    public async ValueTask<IEnumerable<RecurringDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var definitions = new List<RecurringDefinition>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("SELECT * FROM ez_recurring_definitions ORDER BY created_at_utc", conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            definitions.Add(ReadDefinition(reader));
        }

        return definitions;
    }

    public async ValueTask SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand("UPDATE ez_recurring_definitions SET is_active = @is_active WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@is_active", isActive);
        cmd.Parameters.AddWithValue("@id", id.ToString());

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddParameters(SqlCommand cmd, RecurringDefinition def)
    {
        cmd.Parameters.AddWithValue("@id", def.Id.ToString());
        cmd.Parameters.AddWithValue("@type_name", def.TypeName);
        cmd.Parameters.AddWithValue("@method_name", def.MethodName);
        cmd.Parameters.AddWithValue("@argument_types", JsonSerializer.Serialize(def.ArgumentTypes, JsonOptions));
        cmd.Parameters.AddWithValue("@arguments", JsonSerializer.Serialize(def.Arguments, JsonOptions));
        cmd.Parameters.AddWithValue("@cron_expression", def.CronExpression);
        cmd.Parameters.AddWithValue("@is_active", def.IsActive);
        cmd.Parameters.AddWithValue("@created_at_utc", def.CreatedAtUtc);
        cmd.Parameters.AddWithValue("@last_execution_utc", (object?)def.LastExecutionUtc ?? DBNull.Value);
    }

    private static RecurringDefinition ReadDefinition(SqlDataReader reader)
    {
        return new RecurringDefinition(
            Id: Guid.Parse(reader.GetString(0)),
            TypeName: reader.GetString(1),
            MethodName: reader.GetString(2),
            ArgumentTypes: JsonSerializer.Deserialize<string[]>(reader.GetString(3), JsonOptions) ?? [],
            Arguments: JsonSerializer.Deserialize<object?[]>(reader.GetString(4), JsonOptions) ?? [],
            CronExpression: reader.GetString(5),
            IsActive: reader.GetBoolean(6),
            CreatedAtUtc: reader.GetDateTime(7),
            LastExecutionUtc: reader.IsDBNull(8) ? null : reader.GetDateTime(8));
    }
}
