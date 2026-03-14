using CheapHelpers.Models.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CheapHelpers.EF.Infrastructure
{
    /// <summary>
    /// Provides auditing support for any DbContext that uses IAuditable entities.
    /// Call <see cref="ApplyAuditTimestamps"/> in your SaveChanges/SaveChangesAsync override.
    /// </summary>
    public static class AuditHelper
    {
        /// <summary>
        /// Sets CreatedAt and UpdatedAt on all tracked IAuditable entities that are Added or Modified.
        /// </summary>
        public static void ApplyAuditTimestamps(ChangeTracker changeTracker)
        {
            var auditableEntries = changeTracker.Entries()
                .Where(e => e.Entity is IAuditable &&
                           e.State is EntityState.Added or EntityState.Modified);

            if (!auditableEntries.Any()) return;

            var now = DateTime.UtcNow;

            foreach (var entry in auditableEntries)
            {
                var auditable = (IAuditable)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                }

                auditable.UpdatedAt = now;
            }
        }
    }
}
