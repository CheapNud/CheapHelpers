using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CheapHelpers.EF.Extensions;

/// <summary>
/// Startup helpers that bring a database schema up to date.
/// </summary>
public static class DatabaseInitializationExtensions
{
    private const string LoggerCategory = "CheapHelpers.EF.DatabaseInitialization";

    /// <summary>
    /// Applies pending migrations when <typeparamref name="TContext"/> has any, otherwise creates the database from the model.
    /// Resolves the context from a new scope, or from <see cref="IDbContextFactory{TContext}"/> when the context itself is not registered.
    /// </summary>
    public static async Task MigrateOrEnsureCreatedAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetService<ILoggerFactory>()?.CreateLogger(LoggerCategory);

        var context = provider.GetService<TContext>();
        // A context from the factory is ours to dispose; a scoped one is disposed with the scope.
        await using var ownedContext = context is null
            ? await provider.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContextAsync(cancellationToken)
            : null;
        context ??= ownedContext!;

        var contextName = typeof(TContext).Name;

        if (context.Database.GetMigrations().Any())
        {
            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            Log(logger, pending.Count == 0
                ? $"{contextName}: no pending migrations"
                : $"{contextName}: applying {pending.Count} pending migration(s): {string.Join(", ", pending)}");
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            var created = await context.Database.EnsureCreatedAsync(cancellationToken);
            Log(logger, created
                ? $"{contextName}: no migrations found, created the database from the model"
                : $"{contextName}: no migrations found, the database already exists");
        }
    }

    /// <summary>
    /// Applies pending migrations or creates the database, using the host's services.
    /// </summary>
    public static Task MigrateOrEnsureCreatedAsync<TContext>(this IHost host, CancellationToken cancellationToken = default)
        where TContext : DbContext
        => host.Services.MigrateOrEnsureCreatedAsync<TContext>(cancellationToken);

    private static void Log(ILogger? logger, string message)
    {
        if (logger is null)
            Debug.WriteLine(message);
        else
            logger.LogInformation("{Message}", message);
    }
}
