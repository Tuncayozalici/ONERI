using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ONERI.Models
{
    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AdSoyad { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class RoleListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int PermissionCount { get; set; }
        public bool IsSuperAdmin { get; set; }
    }

    public class RoleOptionViewModel
    {
        public string RoleName { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public class PermissionOptionViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public class UserEditViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AdSoyad { get; set; }
        public string? Email { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<RoleOptionViewModel> Roles { get; set; } = new();
        public List<PermissionOptionViewModel> ExtraPermissions { get; set; } = new();
    }

    public class RoleEditViewModel
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public List<PermissionOptionViewModel> Permissions { get; set; } = new();
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public List<RoleOptionViewModel> Roles { get; set; } = new();
    }
}
