using CheapHelpers;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CheapHelpers.EF.Infrastructure;

namespace CheapHelpers.EF;

/// <summary>
/// Context with Identity + notifications and file attachments.
/// Use this for apps that need user-facing communication features.
/// <para>Inheritance chain: <c>CheapContext → CheapCommunicationContext → CheapBusinessContext</c></para>
/// </summary>
public class CheapCommunicationContext<TUser> : CheapContext<TUser> where TUser : IdentityUser
{
    public CheapCommunicationContext(
        DbContextOptions<CheapCommunicationContext<TUser>> options,
        CheapContextOptions? contextOptions = null)
        : base(options, contextOptions) { }

    protected CheapCommunicationContext(DbContextOptions options, CheapContextOptions? contextOptions = null)
        : base(options, contextOptions) { }

    public DbSet<InAppNotification> InAppNotifications { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    public DbSet<FileAttachment> FileAttachments { get; set; }
    public DbSet<GenericFileAttachment> GenericFileAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var utcNow = GetUtcNowFunction();

        modelBuilder.Entity<FileAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            entity.Property(e => e.FileExtension).HasMaxLength(10);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(1000);
            entity.Property(e => e.CreatedById).HasMaxLength(450);
            entity.Property(e => e.UpdatedById).HasMaxLength(450);
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName(Constants.Database.FileAttachmentsCreatedAtIndex);
            entity.HasIndex(e => e.Visible).HasDatabaseName(Constants.Database.FileAttachmentsVisibleIndex);
            entity.HasIndex(e => e.MimeType).HasDatabaseName(Constants.Database.FileAttachmentsMimeTypeIndex);
            entity.Property(e => e.Visible).HasDefaultValue(true);
            entity.Property(e => e.DisplayIndex).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql(utcNow);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql(utcNow);
        });

        modelBuilder.Entity<GenericFileAttachment>(entity =>
        {
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.HasIndex(e => new { e.EntityId, e.EntityType })
                  .HasDatabaseName(Constants.Database.FileAttachmentsEntityIndex);
        });

        modelBuilder.Entity<InAppNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NotificationType).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
            entity.HasIndex(e => e.UserId).HasDatabaseName(Constants.Database.NotificationsUserIdIndex);
            entity.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName(Constants.Database.NotificationsUserIdIsReadIndex);
            entity.HasIndex(e => new { e.UserId, e.NotificationType }).HasDatabaseName(Constants.Database.NotificationsUserIdTypeIndex);
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName(Constants.Database.NotificationsCreatedAtIndex);
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName(Constants.Database.NotificationsExpiresAtIndex);
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.NotificationType })
                  .IsUnique()
                  .HasDatabaseName(Constants.Database.NotificationPreferencesUserIdTypeIndex);
        });
    }
}
