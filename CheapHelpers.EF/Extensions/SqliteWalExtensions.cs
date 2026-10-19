using CheapHelpers.EF.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.EF.Extensions;

/// <summary>
/// Registers <see cref="SqliteWalInterceptor"/> on a context's options.
/// </summary>
public static class SqliteWalExtensions
{
    /// <summary>
    /// Puts SQLite databases in WAL journal mode on first open. Has no effect on other providers.
    /// </summary>
    public static DbContextOptionsBuilder UseSqliteWal(this DbContextOptionsBuilder builder)
        => builder.AddInterceptors(new SqliteWalInterceptor());

    /// <summary>
    /// Puts SQLite databases in WAL journal mode on first open, keeping the typed builder. Has no effect on other providers.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseSqliteWal<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
        => builder.AddInterceptors(new SqliteWalInterceptor());
}
