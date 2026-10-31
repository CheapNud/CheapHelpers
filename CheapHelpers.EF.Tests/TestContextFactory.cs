using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.EF.Tests;

public sealed class TestContextFactory(SqliteConnection connection) : IDbContextFactory<TestJobDbContext>
{
    public TestJobDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TestJobDbContext>().UseSqlite(connection).Options);
}
