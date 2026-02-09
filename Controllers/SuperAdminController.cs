using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONERI.Models;
using ONERI.Models.Authorization;
using ONERI.Services.SuperAdmin;

namespace ONERI.Controllers
{
    [Authorize(Roles = Permissions.SuperAdminRole)]
    public class SuperAdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISuperAdminQueryService _superAdminQueryService;

        public SuperAdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ISuperAdminQueryService superAdminQueryService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _superAdminQueryService = superAdminQueryService;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _superAdminQueryService.GetUsersWithRolesAsync();
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
            var userRoleSet = userRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
                IsSuperAdmin = userRoleSet.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase)
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
                    Selected = userRoleSet.Contains(role.Name, StringComparer.OrdinalIgnoreCase)
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

            var canonicalRoleMap = await BuildRoleCanonicalMapAsync();
            var selectedRoles = model.Roles
                .Where(r => r.Selected)
                .Select(r => CanonicalizeRoleName(r.RoleName, canonicalRoleMap))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var currentRoles = (await _userManager.GetRolesAsync(user)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var isSuperAdminUser = currentRoles.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase);
            if (isSuperAdminUser && !selectedRoles.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Super Admin rolü kaldırılmaz.");
                await RebuildUserEditModel(model);
                return View(model);
            }

            if (!isSuperAdminUser && selectedRoles.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase))
            {
                var existingSuperAdmins = await _userManager.GetUsersInRoleAsync(Permissions.SuperAdminRole);
                if (existingSuperAdmins.Any(u => u.Id != user.Id))
                {
                    ModelState.AddModelError(string.Empty, "Sistemde zaten bir Super Admin bulunuyor. İkinci bir Super Admin atanamaz.");
                    await RebuildUserEditModel(model);
                    return View(model);
                }
            }

            var rolesToAdd = selectedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToRemove = currentRoles.Except(selectedRoles, StringComparer.OrdinalIgnoreCase).ToList();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Yeni şifre ve doğrulama zorunludur.");
            }
            else if (!string.Equals(model.NewPassword, model.ConfirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Şifreler eşleşmiyor.");
            }

            if (ModelState.IsValid)
            {
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validation = await validator.ValidateAsync(_userManager, user, model.NewPassword!);
                    if (!validation.Succeeded)
                    {
                        foreach (var error in validation.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await RebuildUserEditModel(model);
                return View("EditUser", model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword!);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await RebuildUserEditModel(model);
                return View("EditUser", model);
            }

            TempData["Success"] = "Şifre güncellendi.";
            return RedirectToAction(nameof(EditUser), new { id = user.Id });
        }

        public async Task<IActionResult> Roles()
        {
            var list = await _superAdminQueryService.GetRolesWithPermissionCountsAsync();
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
                    Selected = string.Equals(role.Name, "Personel", StringComparison.OrdinalIgnoreCase)
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

            var canonicalRoleMap = await BuildRoleCanonicalMapAsync();
            var selectedRoles = model.Roles
                .Where(r => r.Selected)
                .Select(r => CanonicalizeRoleName(r.RoleName, canonicalRoleMap))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (selectedRoles.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase))
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

            if (string.Equals(role.Name, Permissions.SuperAdminRole, StringComparison.OrdinalIgnoreCase))
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

            if (string.Equals(role.Name, Permissions.SuperAdminRole, StringComparison.OrdinalIgnoreCase))
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
            var userRoleSet = userRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);

            model.UserName = user.UserName ?? string.Empty;
            model.AdSoyad = user.AdSoyad;
            model.Email = user.Email;
            model.IsSuperAdmin = userRoleSet.Contains(Permissions.SuperAdminRole, StringComparer.OrdinalIgnoreCase);

            model.Roles = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RoleOptionViewModel
                {
                    RoleName = r.Name!,
                    Selected = userRoleSet.Contains(r.Name!, StringComparer.OrdinalIgnoreCase)
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
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            model.Roles = allRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RoleOptionViewModel
                {
                    RoleName = r.Name!,
                    Selected = selected.Contains(r.Name!)
                })
                .ToList();
        }

        private async Task<Dictionary<string, string>> BuildRoleCanonicalMapAsync()
        {
            var roleNames = await _roleManager.Roles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => r.Name!)
                .ToListAsync();

            return roleNames.ToDictionary(r => r, r => r, StringComparer.OrdinalIgnoreCase);
        }

        private static string CanonicalizeRoleName(string roleName, IReadOnlyDictionary<string, string> canonicalRoleMap)
        {
            return canonicalRoleMap.TryGetValue(roleName, out var canonical) ? canonical : roleName;
        }
    }
}
