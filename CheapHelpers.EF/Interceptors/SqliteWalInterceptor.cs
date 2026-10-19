using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data.Common;

namespace CheapHelpers.EF.Interceptors;

/// <summary>
/// Switches SQLite databases to WAL journal mode when a connection opens. Connections of any other provider are left alone.
/// </summary>
public sealed class SqliteWalInterceptor : DbConnectionInterceptor
{
    // Matched by name so this package needs no SQLite reference.
    private const string SqliteConnectionTypeName = "Microsoft.Data.Sqlite.SqliteConnection";
    private const string WalPragma = "PRAGMA journal_mode=WAL;";
    private const string ModeKeyword = "Mode";
    private const string ReadOnlyMode = "ReadOnly";

    // WAL mode is stored in the database file, so one pragma per database is enough. Without this the
    // pragma would run again on every open of a pooled connection. Static because AddDbContext builds
    // new options, and so a new interceptor, for every scope.
    // A database file deleted and recreated under the same connection string in the same process keeps
    // the default journal mode until restart.
    private static readonly ConcurrentDictionary<string, byte> HandledConnectionStrings = new();

    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (!NeedsPragma(connection))
            return;

        using var command = connection.CreateCommand();
        command.CommandText = WalPragma;
        command.ExecuteNonQuery();
        HandledConnectionStrings.TryAdd(connection.ConnectionString, 0);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!NeedsPragma(connection))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = WalPragma;
        await command.ExecuteNonQueryAsync(cancellationToken);
        HandledConnectionStrings.TryAdd(connection.ConnectionString, 0);
    }

    private static bool NeedsPragma(DbConnection connection)
    {
        if (connection.GetType().FullName != SqliteConnectionTypeName
            || HandledConnectionStrings.ContainsKey(connection.ConnectionString))
            return false;

        // EF opens a Mode=ReadOnly connection to check whether the database exists; the pragma would fail
        // there with "attempt to write a readonly database". The writable connection that follows sets WAL.
        if (IsReadOnly(connection.ConnectionString))
        {
            HandledConnectionStrings.TryAdd(connection.ConnectionString, 0);
            return false;
        }

        return true;
    }

    private static bool IsReadOnly(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.TryGetValue(ModeKeyword, out var mode)
            && string.Equals(mode?.ToString(), ReadOnlyMode, StringComparison.OrdinalIgnoreCase);
    }
}
