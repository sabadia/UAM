using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Slogtry.Abstractions;
using Slogtry.Infrastructure;
using UAM.Context;
using UAM.Models;
using Xunit;

namespace UAM.Tests;

public class HttpContextTenantProviderTests
{
    [Fact]
    public void GetRequiredTenantId_ReturnsTenantIdFromContext()
    {
        var accessor = new TenantContextAccessor();
        accessor.SetCurrent(new TenantContext("tenant-a", "user-1", [], []));

        var provider = new HttpContextTenantProvider(accessor);

        provider.GetRequiredTenantId().Should().Be("tenant-a");
    }

    [Fact]
    public void GetRequiredTenantId_ThrowsWhenContextNotSet()
    {
        var accessor = new TenantContextAccessor();

        var provider = new HttpContextTenantProvider(accessor);

        Action act = () => provider.GetRequiredTenantId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Tenant context*");
    }
}

public class AppDbContextTests
{
    [Fact]
    public void SaveChanges_PopulatesTenantAndAuditFields()
    {
        using var context = CreateContext("tenant-a", Guid.NewGuid().ToString());

        var content = new UserProfile
        {
            ExternalAuthUserId = "external-user-1",
            Email = "user1@example.com",
            DisplayName = "Hello User"
        };

        context.Users.Add(content);
        context.SaveChanges();

        content.TenantId.Should().Be("tenant-a");
        content.CreatedBy.Should().Be("system");
        content.UpdatedBy.Should().Be("system");
        content.CreatedAt.Should().NotBe(default);
        content.UpdatedAt.Should().NotBe(default);
        content.UpdatedAt.Should().BeOnOrAfter(content.CreatedAt);
    }

    [Fact]
    public void QueryFilter_OnlyReturnsRowsForTheCurrentTenant()
    {
        var databaseName = Guid.NewGuid().ToString();

        using (var tenantAContext = CreateContext("tenant-a", databaseName))
        {
            tenantAContext.Users.Add(new UserProfile
            {
                ExternalAuthUserId = "external-user-2",
                Email = "tenant-a@example.com",
                DisplayName = "Tenant A User"
            });
            tenantAContext.SaveChanges();
        }

        using var tenantBContext = CreateContext("tenant-b", databaseName);

        tenantBContext.Users.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_HidesRowUntilRestored()
    {
        using var context = CreateContext("tenant-a", Guid.NewGuid().ToString());

        var content = new UserProfile
        {
            ExternalAuthUserId = "external-user-3",
            Email = "soft-deleted@example.com",
            DisplayName = "Soft deleted user"
        };

        context.Users.Add(content);
        context.SaveChanges();

        content.MarkDeleted("system", DateTimeOffset.UtcNow);
        context.SaveChanges();

        context.Users.Should().BeEmpty();

        content.Restore();
        context.SaveChanges();

        context.Users.Should().ContainSingle(entity => entity.Id == content.Id);
    }

    private static AppDbContext CreateContext(string tenantId, string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new AppDbContext(options, new FixedTenantProvider(tenantId));
    }

    private sealed class FixedTenantProvider(string tenantId) : ITenantProvider
    {
        public string GetRequiredTenantId() => tenantId;
    }
}
