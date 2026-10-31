using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CheapHelpers.Jobs.Tests;

public sealed class TestJobDbContext(DbContextOptions<TestJobDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyJobQueue();

    // SQLite cannot compare or order DateTimeOffset columns; storing them as numbers keeps the queue queries translatable
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
}
