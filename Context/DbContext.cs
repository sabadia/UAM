using Microsoft.EntityFrameworkCore;
using UAM.Models;

namespace UAM.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
    : SlogtryDbContextBase(options, tenantProvider)
{
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<UserPrivacySettings> PrivacySettings => Set<UserPrivacySettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureBaseModel<UserProfile>(modelBuilder);
        ConfigureBaseModel<UserPrivacySettings>(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.ExternalAuthUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(128);
            entity.Property(e => e.LastName).HasMaxLength(128);
            entity.Property(e => e.PhoneNumber).HasMaxLength(32);

            entity.Property(e => e.PreferencesLanguage).HasMaxLength(16).IsRequired();
            entity.Property(e => e.PreferencesTimeZone).HasMaxLength(64).IsRequired();
            entity.Property(e => e.PreferencesTheme).HasConversion<string>().HasMaxLength(16).IsRequired();

            // Social profile extensions
            entity.Property(e => e.Handle).HasMaxLength(30);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.AvatarFileId).HasMaxLength(26);
            entity.Property(e => e.CoverFileId).HasMaxLength(26);
            entity.Property(e => e.Website).HasMaxLength(2048);
            entity.Property(e => e.VerifiedBadgeKind).HasMaxLength(32);

            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
            entity.HasIndex(e => new { e.TenantId, e.ExternalAuthUserId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
            entity.HasIndex(e => new { e.TenantId, e.Handle }).IsUnique().HasFilter("\"IsDeleted\" = FALSE AND \"Handle\" IS NOT NULL");
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        modelBuilder.Entity<UserPrivacySettings>(entity =>
        {
            entity.ToTable("UserPrivacySettings");
            entity.Property(e => e.ProfileVisibility).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(e => e.WhoCanMessage).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(e => e.WhoCanMention).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(e => e.UserProfileId).HasMaxLength(26).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.UserProfileId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        });

        modelBuilder.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
        modelBuilder.Entity<UserPrivacySettings>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
    }
}
