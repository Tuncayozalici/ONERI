using ONERI.Models;

namespace ONERI.Services.SuperAdmin;

public interface ISuperAdminQueryService
{
    Task<List<UserListItemViewModel>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);
    Task<List<RoleListItemViewModel>> GetRolesWithPermissionCountsAsync(CancellationToken cancellationToken = default);
}
