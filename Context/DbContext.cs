using Microsoft.EntityFrameworkCore;
using UAM.Models;

namespace UAM.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
    : SlogtryDbContextBase(options, tenantProvider)
{
    public DbSet<UserProfile> Users => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureBaseModel<UserProfile>(modelBuilder);

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

            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
            entity.HasIndex(e => new { e.TenantId, e.ExternalAuthUserId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        modelBuilder.Entity<UserProfile>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
    }
}
