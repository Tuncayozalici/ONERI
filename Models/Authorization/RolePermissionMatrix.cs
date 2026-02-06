using System;
using System.Collections.Generic;
using System.Linq;

namespace ONERI.Models.Authorization
{
    public static class RolePermissionMatrix
    {
        public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> DefaultRolePermissions =
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Yönetici"] = new[]
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
                },
                ["Personel"] = new[]
                {
                    Permissions.FikirAtolyesi.View,
                    Permissions.Oneri.Create,
                    Permissions.Oneri.Query
                }
            };

        public static IReadOnlyCollection<string> GetRoleNames()
        {
            return DefaultRolePermissions.Keys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
