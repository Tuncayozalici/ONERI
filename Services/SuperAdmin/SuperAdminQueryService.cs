using Microsoft.EntityFrameworkCore;
using ONERI.Data;
using ONERI.Models;
using ONERI.Models.Authorization;

namespace ONERI.Services.SuperAdmin;

public class SuperAdminQueryService : ISuperAdminQueryService
{
    private readonly FabrikaContext _context;

    public SuperAdminQueryService(FabrikaContext context)
    {
        _context = context;
    }

    public async Task<List<UserListItemViewModel>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.AdSoyad,
                u.Email
            })
            .ToListAsync(cancellationToken);

        var roleRows = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name != null
            select new
            {
                userRole.UserId,
                RoleName = role.Name!
            })
            .ToListAsync(cancellationToken);

        var roleMap = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RoleName).OrderBy(x => x).ToList());

        return users
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                AdSoyad = u.AdSoyad,
                Email = u.Email,
                Roles = roleMap.TryGetValue(u.Id, out var roles) ? roles : new List<string>()
            })
            .ToList();
    }

    public async Task<List<RoleListItemViewModel>> GetRolesWithPermissionCountsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Where(r => r.Name != null)
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id,
                Name = r.Name!
            })
            .ToListAsync(cancellationToken);

        var permissionCountByRoleId = await _context.RoleClaims
            .AsNoTracking()
            .Where(c => c.ClaimType == Permissions.ClaimType)
            .GroupBy(c => c.RoleId)
            .Select(g => new
            {
                RoleId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles
            .Select(r => new RoleListItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                PermissionCount = permissionCountByRoleId.TryGetValue(r.Id, out var count) ? count : 0,
                IsSuperAdmin = string.Equals(r.Name, Permissions.SuperAdminRole, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }
}
