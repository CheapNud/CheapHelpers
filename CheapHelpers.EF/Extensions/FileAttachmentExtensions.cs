using CheapHelpers.EF.Repositories;
using CheapHelpers.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheapHelpers.EF.Extensions
{
    /// <summary>
    /// Extension methods for FileAttachment operations
    /// </summary>
    public static class FileAttachmentExtensions
    {
        /// <summary>
        /// Gets files for a specific entity
        /// </summary>
        public static async Task<List<T>> GetFilesForEntityAsync<T>(
            this BaseRepo repo,
            int entityId,
            string entityType,
            bool visibleOnly = true,
            CancellationToken token = default)
            where T : GenericFileAttachment
        {
            try
            {
                using var context = repo._factory.CreateDbContext();
                var query = context.Set<T>()
                    .Where(f => f.EntityId == entityId && f.EntityType == entityType);

                if (visibleOnly)
                    query = query.Where(f => f.Visible);

                return await query.OrderBy(f => f.DisplayIndex)
                                 .ThenBy(f => f.CreatedAt)
                                 .ToListAsync(token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting files for entity {entityType}:{entityId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Soft deletes a file attachment
        /// </summary>
        public static async Task<bool> SoftDeleteFileAsync<T>(
            this BaseRepo repo,
            int fileId,
            string? userId = null,
            CancellationToken token = default)
            where T : FileAttachment
        {
            try
            {
                using var context = repo._factory.CreateDbContext();
                var file = await context.Set<T>().FindAsync([fileId], token);

                if (file == null) return false;

                file.Visible = false;
                file.MarkAsUpdated(userId);

                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error soft deleting file {fileId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates file metadata
        /// </summary>
        public static async Task<bool> UpdateFileMetadataAsync<T>(
            this BaseRepo repo,
            int fileId,
            Action<T> updateAction,
            string? userId = null,
            CancellationToken token = default)
            where T : FileAttachment
        {
            try
            {
                using var context = repo._factory.CreateDbContext();
                var file = await context.Set<T>().FindAsync([fileId], token);

                if (file == null) return false;

                updateAction(file);
                file.MarkAsUpdated(userId);

                await context.SaveChangesAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating file metadata {fileId}: {ex.Message}");
                throw;
            }
        }
    }
}
