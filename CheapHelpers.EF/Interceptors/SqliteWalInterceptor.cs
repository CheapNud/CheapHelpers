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
    // SqliteException.SqliteErrorCode; DbException.ErrorCode does not carry the SQLite result code.
    private const string SqliteErrorCodeProperty = "SqliteErrorCode";
    // SQLITE_READONLY, "attempt to write a readonly database".
    private const int SqliteReadOnlyErrorCode = 8;

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
        try
        {
            command.ExecuteNonQuery();
        }
        catch (DbException ex) when (IsReadOnlyError(ex))
        {
            // Read-only connection, see IsReadOnlyError.
        }
        HandledConnectionStrings.TryAdd(connection.ConnectionString, 0);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!NeedsPragma(connection))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = WalPragma;
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (DbException ex) when (IsReadOnlyError(ex))
        {
            // Read-only connection, see IsReadOnlyError.
        }
        HandledConnectionStrings.TryAdd(connection.ConnectionString, 0);
    }

    private static bool NeedsPragma(DbConnection connection)
        => connection.GetType().FullName == SqliteConnectionTypeName
            && !HandledConnectionStrings.ContainsKey(connection.ConnectionString);

    // A read-only connection cannot change the journal mode: EF's Mode=ReadOnly probe that checks whether the
    // database exists, a file: URI with mode=ro, or a file without write access. The pragma fails there with
    // SQLITE_READONLY and is skipped; a writable connection to the same database sets WAL.
    private static bool IsReadOnlyError(DbException exception)
        => exception.GetType().GetProperty(SqliteErrorCodeProperty)?.GetValue(exception) is SqliteReadOnlyErrorCode;
}
