using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Testing;

/// <summary>
/// An in-memory SQLite database for one test. The database lives as long as the connection, and every context
/// handed out shares that connection, so they all see the same rows. Dispose it to drop the database.
/// <c>CreateDbContextAsync</c> comes from the default implementation on <see cref="IDbContextFactory{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The context type under test.</typeparam>
public sealed class SqliteTestDatabase<TContext> : IDbContextFactory<TContext>, IDisposable
    where TContext : DbContext
{
    private const string ConnectionString = "Data Source=:memory:";

    private readonly SqliteConnection _connection;
    private readonly Func<DbContextOptions<TContext>, TContext> _createContext;

    /// <summary>Opens the connection and creates the schema.</summary>
    /// <param name="createContext">Builds a context from the options, usually <c>options =&gt; new MyContext(options)</c>.</param>
    /// <param name="migrate">True runs the context's migrations; false creates the schema straight from the model.</param>
    public SqliteTestDatabase(Func<DbContextOptions<TContext>, TContext> createContext, bool migrate = false)
    {
        _createContext = createContext;
        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();
        Options = new DbContextOptionsBuilder<TContext>().UseSqlite(_connection).Options;

        using var context = CreateContext();
        if (migrate)
        {
            context.Database.Migrate();
        }
        else
        {
            context.Database.EnsureCreated();
        }
    }

    /// <summary>The options every context is built with.</summary>
    public DbContextOptions<TContext> Options { get; }

    /// <summary>Creates a new context on the shared connection. The caller disposes it.</summary>
    public TContext CreateContext() => _createContext(Options);

    /// <inheritdoc cref="CreateContext" />
    TContext IDbContextFactory<TContext>.CreateDbContext() => CreateContext();

    /// <summary>Closes the connection, which drops the database.</summary>
    public void Dispose() => _connection.Dispose();
}
