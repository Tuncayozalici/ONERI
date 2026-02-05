using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Models;
using ONERI.Models.Authorization;

namespace ONERI.Controllers
{
    [Authorize(Roles = Permissions.SuperAdminRole)]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SuperAdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var items = new List<UserListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    AdSoyad = user.AdSoyad,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var userRoles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);
            var userPermissionClaims = userClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            var model = new UserEditViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                AdSoyad = user.AdSoyad,
                Email = user.Email,
                IsSuperAdmin = userRoles.Contains(Permissions.SuperAdminRole)
            };

            foreach (var role in allRoles)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                model.Roles.Add(new RoleOptionViewModel
                {
                    RoleName = role.Name,
                    Selected = userRoles.Contains(role.Name)
                });
            }

            foreach (var permission in Permissions.All.OrderBy(p => p.Group).ThenBy(p => p.Name))
            {
                model.ExtraPermissions.Add(new PermissionOptionViewModel
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Group = permission.Group,
                    Selected = userPermissionClaims.Contains(permission.Key)
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RebuildUserEditModel(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var selectedRoles = model.Roles
                .Where(r => r.Selected)
                .Select(r => r.RoleName)
                .ToHashSet();

            var currentRoles = await _userManager.GetRolesAsync(user);

            var isSuperAdminUser = currentRoles.Contains(Permissions.SuperAdminRole);
            if (isSuperAdminUser && !selectedRoles.Contains(Permissions.SuperAdminRole))
            {
                ModelState.AddModelError(string.Empty, "Super Admin rolü kaldırılmaz.");
                await RebuildUserEditModel(model);
                return View(model);
            }

            if (!isSuperAdminUser && selectedRoles.Contains(Permissions.SuperAdminRole))
            {
                var existingSuperAdmins = await _userManager.GetUsersInRoleAsync(Permissions.SuperAdminRole);
                if (existingSuperAdmins.Any(u => u.Id != user.Id))
                {
                    ModelState.AddModelError(string.Empty, "Sistemde zaten bir Super Admin bulunuyor. İkinci bir Super Admin atanamaz.");
                    await RebuildUserEditModel(model);
                    return View(model);
                }
            }

            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

            if (rolesToAdd.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (rolesToRemove.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }

            var existingClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = existingClaims.Where(c => c.Type == Permissions.ClaimType).ToList();
            foreach (var claim in permissionClaims)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            var selectedPermissions = model.ExtraPermissions
                .Where(p => p.Selected)
                .Select(p => p.Key)
                .Distinct();

            foreach (var permission in selectedPermissions)
            {
                await _userManager.AddClaimAsync(user, new Claim(Permissions.ClaimType, permission));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = "Kullanıcı yetkileri güncellendi.";
            return RedirectToAction(nameof(EditUser), new { id = user.Id });
        }

        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var list = new List<RoleListItemViewModel>();
            foreach (var role in roles)
            {
                if (role.Name == null)
                {
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                var permissionCount = claims.Count(c => c.Type == Permissions.ClaimType);
                list.Add(new RoleListItemViewModel
                {
                    Id = role.Id,
                    Name = role.Name,
                    PermissionCount = permissionCount,
                    IsSuperAdmin = role.Name == Permissions.SuperAdminRole
                });
            }

            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var model = new CreateUserViewModel();
            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                model.Roles.Add(new RoleOptionViewModel
                {
                    RoleName = role.Name,
                    Selected = role.Name == "Personel"
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RebuildCreateUserModel(model);
                return View(model);
            }

            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Bu kullanıcı adı zaten kullanılıyor.");
                await RebuildCreateUserModel(model);
                return View(model);
            }

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Bu e-posta zaten kullanılıyor.");
                await RebuildCreateUserModel(model);
                return View(model);
            }

            var selectedRoles = model.Roles
                .Where(r => r.Selected)
                .Select(r => r.RoleName)
                .ToHashSet();

            if (selectedRoles.Contains(Permissions.SuperAdminRole))
            {
                var existingSuperAdmins = await _userManager.GetUsersInRoleAsync(Permissions.SuperAdminRole);
                if (existingSuperAdmins.Count > 0)
                {
                    ModelState.AddModelError(string.Empty, "Sistemde zaten bir Super Admin var. İkinci bir Super Admin oluşturulamaz.");
                    await RebuildCreateUserModel(model);
                    return View(model);
                }
            }

            if (selectedRoles.Count == 0)
            {
                selectedRoles.Add("Personel");
            }

            var user = new AppUser
            {
                UserName = model.UserName,
                Email = model.Email,
                AdSoyad = model.AdSoyad
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await RebuildCreateUserModel(model);
                return View(model);
            }

            var addRolesResult = await _userManager.AddToRolesAsync(user, selectedRoles);
            if (!addRolesResult.Succeeded)
            {
                foreach (var error in addRolesResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await RebuildCreateUserModel(model);
                return View(model);
            }

            TempData["Success"] = "Kullanıcı oluşturuldu.";
            return RedirectToAction(nameof(EditUser), new { id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Rol adı boş olamaz.";
                return RedirectToAction(nameof(Roles));
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = "Bu rol zaten mevcut.";
                return RedirectToAction(nameof(Roles));
            }

            await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            TempData["Success"] = "Rol oluşturuldu.";
            return RedirectToAction(nameof(Roles));
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null || role.Name == null)
            {
                return NotFound();
            }

            if (role.Name == Permissions.SuperAdminRole)
            {
                TempData["Error"] = "Super Admin rolü tüm yetkilere sahiptir ve burada düzenlenmez.";
                return RedirectToAction(nameof(Roles));
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var rolePermissions = existingClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            var model = new RoleEditViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name
            };

            foreach (var permission in Permissions.All.OrderBy(p => p.Group).ThenBy(p => p.Name))
            {
                model.Permissions.Add(new PermissionOptionViewModel
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Group = permission.Group,
                    Selected = rolePermissions.Contains(permission.Key)
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(RoleEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null || role.Name == null)
            {
                return NotFound();
            }

            if (role.Name == Permissions.SuperAdminRole)
            {
                TempData["Error"] = "Super Admin rolü tüm yetkilere sahiptir ve burada düzenlenmez.";
                return RedirectToAction(nameof(Roles));
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == Permissions.ClaimType).ToList();
            foreach (var claim in permissionClaims)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            var selectedPermissions = model.Permissions
                .Where(p => p.Selected)
                .Select(p => p.Key)
                .Distinct();

            foreach (var permission in selectedPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            }

            TempData["Success"] = "Rol yetkileri güncellendi.";
            return RedirectToAction(nameof(EditRole), new { id = role.Id });
        }

        private async Task RebuildUserEditModel(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return;
            }

            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var userRoles = await _userManager.GetRolesAsync(user);

            model.UserName = user.UserName ?? string.Empty;
            model.AdSoyad = user.AdSoyad;
            model.Email = user.Email;
            model.IsSuperAdmin = userRoles.Contains(Permissions.SuperAdminRole);

            model.Roles = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RoleOptionViewModel
                {
                    RoleName = r.Name!,
                    Selected = userRoles.Contains(r.Name!)
                })
                .ToList();

            var userClaims = await _userManager.GetClaimsAsync(user);
            var userPermissionClaims = userClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            model.ExtraPermissions = Permissions.All
                .OrderBy(p => p.Group)
                .ThenBy(p => p.Name)
                .Select(p => new PermissionOptionViewModel
                {
                    Key = p.Key,
                    Name = p.Name,
                    Group = p.Group,
                    Selected = userPermissionClaims.Contains(p.Key)
                })
                .ToList();
        }

        private async Task RebuildCreateUserModel(CreateUserViewModel model)
        {
            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var selected = model.Roles
                .Where(r => r.Selected)
                .Select(r => r.RoleName)
                .ToHashSet();

            model.Roles = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RoleOptionViewModel
                {
                    RoleName = r.Name!,
                    Selected = selected.Contains(r.Name!)
                })
                .ToList();
        }
    }
}
