using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.EF.Infrastructure;

/// <summary>
/// Adapts an <see cref="IDbContextFactory{TDerived}"/> to <see cref="IDbContextFactory{TBase}"/>
/// to work around the lack of covariance on the interface.
/// </summary>
public class DbContextFactoryAdapter<TBase, TDerived>(IDbContextFactory<TDerived> inner) : IDbContextFactory<TBase>
    where TBase : DbContext
    where TDerived : TBase
{
    public TBase CreateDbContext() => inner.CreateDbContext();
}
