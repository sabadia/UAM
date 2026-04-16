using Microsoft.EntityFrameworkCore;
using UAM.Models;

namespace UAM.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : DbContext(options)
{
    private const string SystemUser = "system";
    private readonly string _tenantId = tenantProvider.GetRequiredTenantId();

    public DbSet<Content> Story => Set<Content>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureBaseModel<Content>(modelBuilder);

        modelBuilder.Entity<Content>(entity =>
        {
            entity.ToTable("Stories");
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.Value).HasColumnType("text");
            entity.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        });

        modelBuilder.Entity<Content>().HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);
    }

    public override int SaveChanges()
    {
        ApplyTenantAndAuditValues();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantAndAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantAndAuditValues()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseModel>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantId;
                entry.Entity.CreatedAt = entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt;
                entry.Entity.UpdatedAt = now;
                entry.Entity.CreatedBy = string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) ? SystemUser : entry.Entity.CreatedBy;
                entry.Entity.UpdatedBy = string.IsNullOrWhiteSpace(entry.Entity.UpdatedBy) ? entry.Entity.CreatedBy : entry.Entity.UpdatedBy;
                entry.Entity.IsDeleted = false;
                entry.Entity.DeletedAt = null;
                entry.Entity.DeletedBy = null;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.TenantId = _tenantId;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = string.IsNullOrWhiteSpace(entry.Entity.UpdatedBy) ? SystemUser : entry.Entity.UpdatedBy;

                if (entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt ??= now;
                    entry.Entity.DeletedBy = string.IsNullOrWhiteSpace(entry.Entity.DeletedBy) ? entry.Entity.UpdatedBy : entry.Entity.DeletedBy;
                }
                else
                {
                    entry.Entity.DeletedAt = null;
                    entry.Entity.DeletedBy = null;
                }

                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedBy).IsModified = false;
                entry.Property(entity => entity.TenantId).IsModified = false;
            }
        }
    }

    private static void ConfigureBaseModel<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseModel
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).ValueGeneratedNever().IsRequired();
            entity.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(128).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Id });
        });
    }
}
