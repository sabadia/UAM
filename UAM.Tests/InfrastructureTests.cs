using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UAM.Context;
using UAM.Models;
using UAM.Security;
using Xunit;

namespace UAM.Tests;

public class HttpContextTenantProviderTests
{
    [Fact]
    public void GetRequiredTenantId_ReturnsTrimmedHeaderValue()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        httpContextAccessor.HttpContext!.Request.Headers["X-Tenant-Id"] = " tenant-a ";

        var provider = new HttpContextTenantProvider(httpContextAccessor, new TenantContextAccessor());

        provider.GetRequiredTenantId().Should().Be("tenant-a");
    }

    [Fact]
    public void GetRequiredTenantId_ThrowsWhenHeaderIsMissing()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };

        var provider = new HttpContextTenantProvider(httpContextAccessor, new TenantContextAccessor());

        Action act = () => provider.GetRequiredTenantId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*X-Tenant-Id*");
    }
}

public class AppDbContextTests
{
    [Fact]
    public void SaveChanges_PopulatesTenantAndAuditFields()
    {
        using var context = CreateContext("tenant-a", Guid.NewGuid().ToString());

        var content = new Content
        {
            Title = "Hello world",
            Slug = "hello-world"
        };

        context.Story.Add(content);
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
            tenantAContext.Story.Add(new Content
            {
                Title = "Tenant A story",
                Slug = "tenant-a-story"
            });
            tenantAContext.SaveChanges();
        }

        using var tenantBContext = CreateContext("tenant-b", databaseName);

        tenantBContext.Story.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_HidesRowUntilRestored()
    {
        using var context = CreateContext("tenant-a", Guid.NewGuid().ToString());

        var content = new Content
        {
            Title = "Soft deleted story",
            Slug = "soft-deleted-story"
        };

        context.Story.Add(content);
        context.SaveChanges();

        content.MarkDeleted("system", DateTimeOffset.UtcNow);
        context.SaveChanges();

        context.Story.Should().BeEmpty();

        content.Restore();
        context.SaveChanges();

        context.Story.Should().ContainSingle(entity => entity.Id == content.Id);
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
