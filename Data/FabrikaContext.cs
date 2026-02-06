using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ONERI.Models;

namespace ONERI.Data
{
    public class FabrikaContext : IdentityDbContext<AppUser>
    {
        public FabrikaContext(DbContextOptions<FabrikaContext> options) : base(options)
        {
        }

        public DbSet<Oneri> Oneriler { get; set; }
        public DbSet<BolumYonetici> BolumYoneticileri { get; set; }
        public DbSet<Degerlendirme> Degerlendirmeler { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Oneri>(entity =>
            {
                entity.HasIndex(x => x.TrackingToken).IsUnique();
            });

            builder.Entity<Degerlendirme>(entity =>
            {
                entity.HasIndex(x => x.OneriId).IsUnique();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Degerlendirme_GayretPuani_Range", "[GayretPuani] BETWEEN 0 AND 25");
                    t.HasCheckConstraint("CK_Degerlendirme_OrijinallikPuani_Range", "[OrijinallikPuani] BETWEEN 0 AND 25");
                    t.HasCheckConstraint("CK_Degerlendirme_EtkiPuani_Range", "[EtkiPuani] BETWEEN 0 AND 25");
                    t.HasCheckConstraint("CK_Degerlendirme_UygulanabilirlikPuani_Range", "[UygulanabilirlikPuani] BETWEEN 0 AND 25");
                    t.HasCheckConstraint("CK_Degerlendirme_ToplamPuan_Range", "[ToplamPuan] BETWEEN 0 AND 100");
                    t.HasCheckConstraint(
                        "CK_Degerlendirme_ToplamPuan_Consistency",
                        "[ToplamPuan] = [GayretPuani] + [OrijinallikPuani] + [EtkiPuani] + [UygulanabilirlikPuani]");
                });
            });

            builder.Entity<BolumYonetici>(entity =>
            {
                entity.Property(x => x.BolumAdi)
                    .HasMaxLength(100)
                    .UseCollation("NOCASE");

                entity.Property(x => x.YoneticiAdi)
                    .HasMaxLength(120);

                entity.Property(x => x.YoneticiEmail)
                    .HasMaxLength(256)
                    .UseCollation("NOCASE");

                entity.HasIndex(x => x.BolumAdi).IsUnique();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_BolumYonetici_BolumAdi_NotEmpty", "length(trim([BolumAdi])) > 0");
                    t.HasCheckConstraint("CK_BolumYonetici_YoneticiAdi_NotEmpty", "length(trim([YoneticiAdi])) > 0");
                    t.HasCheckConstraint("CK_BolumYonetici_YoneticiEmail_NotEmpty", "length(trim([YoneticiEmail])) > 0");
                });
            });
        }
    }
}
