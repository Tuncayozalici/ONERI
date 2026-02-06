using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ONERI.Extensions;
using ONERI.Models.Authorization;
using Xunit;

namespace ONERI.Tests.Authorization;

public class PolicyRoleMatrixTests
{
    [Fact]
    public void PermissionKeys_AreUnique_AndNotEmpty()
    {
        var keys = Permissions.All.Select(p => p.Key).ToList();

        Assert.NotEmpty(keys);
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(keys, string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void RoleMatrix_ReferencesOnlyKnownPermissions()
    {
        var knownPermissions = Permissions.All
            .Select(p => p.Key)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var roleEntry in RolePermissionMatrix.DefaultRolePermissions)
        {
            Assert.False(string.IsNullOrWhiteSpace(roleEntry.Key));
            Assert.NotEmpty(roleEntry.Value);

            foreach (var permissionKey in roleEntry.Value)
            {
                Assert.Contains(permissionKey, knownPermissions);
            }

            Assert.Equal(
                roleEntry.Value.Count,
                roleEntry.Value.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void RoleMatrix_RegressionSnapshot_IsStable()
    {
        var expectedYonetici = new[]
        {
            Permissions.OneriAdmin.Access,
            Permissions.OneriAdmin.Detail,
            Permissions.OneriAdmin.Approve,
            Permissions.OneriAdmin.Reject,
            Permissions.OneriAdmin.Delete,
            Permissions.Oneri.Evaluate,
            Permissions.BolumYoneticileri.View,
            Permissions.BolumYoneticileri.Create,
            Permissions.BolumYoneticileri.Delete,
            Permissions.VeriYukle.Create
        };

        var expectedPersonel = new[]
        {
            Permissions.FikirAtolyesi.View,
            Permissions.Oneri.Create,
            Permissions.Oneri.Query
        };

        Assert.True(RolePermissionMatrix.DefaultRolePermissions.ContainsKey("Yönetici"));
        Assert.True(RolePermissionMatrix.DefaultRolePermissions.ContainsKey("Personel"));

        var yonetici = RolePermissionMatrix.DefaultRolePermissions["Yönetici"];
        var personel = RolePermissionMatrix.DefaultRolePermissions["Personel"];

        Assert.Equal(
            expectedYonetici.OrderBy(x => x, StringComparer.Ordinal),
            yonetici.OrderBy(x => x, StringComparer.Ordinal));

        Assert.Equal(
            expectedPersonel.OrderBy(x => x, StringComparer.Ordinal),
            personel.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void AuthorizationPolicies_AreRegistered_ForAllPermissions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorizationPolicies();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        foreach (var permission in Permissions.All)
        {
            var policy = options.GetPolicy(permission.Key);
            Assert.NotNull(policy);
        }
    }
}
