using Microsoft.AspNetCore.Identity;

namespace ONERI.Models
{
    // ASP.NET Core Identity'nin standart kullanıcı modelini genişleten sınıf.
    public class AppUser : IdentityUser
    {
        // Kullanıcının adını ve soyadını tutmak için ek bir alan.
        public string? AdSoyad { get; set; }
    }
}
