using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Jobs;

/// <summary>Maps the job table into the app's own DbContext.</summary>
public static class JobModelBuilderExtensions
{
    /// <summary>Maps <see cref="JobRecord"/> to <paramref name="tableName"/>; call from <c>OnModelCreating</c> and add a migration.</summary>
    public static ModelBuilder ApplyJobQueue(this ModelBuilder modelBuilder, string tableName = "Jobs")
    {
        modelBuilder.Entity<JobRecord>(job =>
        {
            job.ToTable(tableName);
            job.HasKey(j => j.Id);
            job.Property(j => j.Kind).HasMaxLength(JobRecord.MaxKindLength);
            job.Property(j => j.LastError).HasMaxLength(JobRecord.MaxErrorLength);
            // Bounded so the recovery index below is valid on providers that cannot index unbounded text
            job.Property(j => j.MachineName).HasMaxLength(JobRecord.MaxMachineNameLength);
            job.HasIndex(j => new { j.Status, j.AvailableAt });
            job.HasIndex(j => new { j.MachineName, j.ProcessId });
        });
        return modelBuilder;
    }
}
