using Microsoft.EntityFrameworkCore;
using ONERI.Models;

namespace ONERI.Data
{
    public class FabrikaContext : DbContext
    {
        public FabrikaContext(DbContextOptions<FabrikaContext> options) : base(options)
        {
        }

        public DbSet<Oneri> Oneriler { get; set; }
        public DbSet<BolumYonetici> BolumYoneticileri { get; set; }
        public DbSet<Degerlendirme> Degerlendirmeler { get; set; }
    }
}
