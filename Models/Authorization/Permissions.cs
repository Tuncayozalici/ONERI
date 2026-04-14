using System.Collections.Generic;

namespace ONERI.Models.Authorization
{
    public record PermissionDefinition(string Key, string Name, string Group);

    public static class Permissions
    {
        public const string ClaimType = "permission";
        public const string SuperAdminRole = "SuperAdmin";

        public static class Dashboards
        {
            public const string GunlukVeriler = "dashboard.gunluk.view";
            public const string ProfilLazer = "dashboard.profillazer.view";
            public const string Boyahane = "dashboard.boyahane.view";
            public const string Pvc = "dashboard.pvc.view";
            public const string Cnc = "dashboard.cnc.view";
            public const string Masterwood = "dashboard.masterwood.view";
            public const string Skipper = "dashboard.skipper.view";
            public const string RoverB = "dashboard.roverb.view";
            public const string Tezgah = "dashboard.tezgah.view";
            public const string Ebatlama = "dashboard.ebatlama.view";
            public const string HataliParca = "dashboard.hataliparca.view";
        }

        public static class GunlukVerilerWidgets
        {
            public const string ToplamModulSayisi = "dashboard.gunluk.widget.toplammodul.view";
            public const string HataliAdet = "dashboard.gunluk.widget.hataliadet.view";
            public const string ToplamDuraklama = "dashboard.gunluk.widget.toplamduraklama.view";
            public const string GenelOeeSkoru = "dashboard.gunluk.widget.geneloee.view";
            public const string MakineBazliOee = "dashboard.gunluk.widget.makineoee.view";
            public const string OeeBilesenleri = "dashboard.gunluk.widget.oeebilesenleri.view";
            public const string BolumBazliPlanaUyum = "dashboard.gunluk.widget.planauyum.view";
            public const string BolumBazliOee = "dashboard.gunluk.widget.bolumoee.view";
            public const string BolumBazliPersonel = "dashboard.gunluk.widget.personel.view";
            public const string ModulTrendi = "dashboard.gunluk.widget.modultrendi.view";
            public const string HataNedenleri = "dashboard.gunluk.widget.hatanedenleri.view";
            public const string BolumBazliHata = "dashboard.gunluk.widget.bolumhata.view";
            public const string HataTrendi = "dashboard.gunluk.widget.hatatrendi.view";
            public const string EnCokHataNedeni = "dashboard.gunluk.widget.encokhatanedeni.view";
            public const string EnCokHataOperatoru = "dashboard.gunluk.widget.encokhataoperatoru.view";
            public const string EnCokHataBolumu = "dashboard.gunluk.widget.encokhatabolumu.view";

            public static readonly IReadOnlyList<string> All = new List<string>
            {
                ToplamModulSayisi,
                HataliAdet,
                ToplamDuraklama,
                GenelOeeSkoru,
                MakineBazliOee,
                OeeBilesenleri,
                BolumBazliPlanaUyum,
                BolumBazliOee,
                BolumBazliPersonel,
                ModulTrendi,
                HataNedenleri,
                BolumBazliHata,
                HataTrendi,
                EnCokHataNedeni,
                EnCokHataOperatoru,
                EnCokHataBolumu
            };
        }

        public static class Oneri
        {
            public const string Create = "oneri.create";
            public const string Query = "oneri.query";
            public const string Evaluate = "oneri.evaluate";
        }

        public static class OneriAdmin
        {
            public const string Access = "oneri.admin.access";
            public const string Detail = "oneri.admin.detail";
            public const string Approve = "oneri.admin.approve";
            public const string Reject = "oneri.admin.reject";
            public const string Delete = "oneri.admin.delete";
        }

        public static class BolumYoneticileri
        {
            public const string View = "bolumyonetici.view";
            public const string Create = "bolumyonetici.create";
            public const string Delete = "bolumyonetici.delete";
        }

        public static class VeriYukle
        {
            public const string Create = "veriyukle.create";
        }

        public static class FikirAtolyesi
        {
            public const string View = "fikiratolyesi.view";
        }

        public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
        {
            new(Dashboards.GunlukVeriler, "Günlük Veriler (Özet)", "Dashboards"),
            new(Dashboards.ProfilLazer, "Metal Bölümü", "Dashboards"),
            new(Dashboards.Boyahane, "Boyahane Bölümü", "Dashboards"),
            new(Dashboards.Pvc, "PVC Bölümü", "Dashboards"),
            new(Dashboards.Cnc, "CNC Bölümü", "Dashboards"),
            new(Dashboards.Masterwood, "Masterwood Makinesi", "Dashboards"),
            new(Dashboards.Skipper, "Skipper Makinesi", "Dashboards"),
            new(Dashboards.RoverB, "Rover-B Makinesi", "Dashboards"),
            new(Dashboards.Tezgah, "Tezgah Bölümü", "Dashboards"),
            new(Dashboards.Ebatlama, "Ebatlama Bölümü", "Dashboards"),
            new(Dashboards.HataliParca, "Hatalı Parça Analizi", "Dashboards"),

            new(GunlukVerilerWidgets.ToplamModulSayisi, "Günlük Veriler: Toplam Modül Sayısı", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.HataliAdet, "Günlük Veriler: Hatalı Adet", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.ToplamDuraklama, "Günlük Veriler: Toplam Duraklama", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.GenelOeeSkoru, "Günlük Veriler: Genel OEE Skoru", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.MakineBazliOee, "Günlük Veriler: Makine Bazlı OEE", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.OeeBilesenleri, "Günlük Veriler: OEE Bileşenleri", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.BolumBazliPlanaUyum, "Günlük Veriler: Bölüm Bazlı Plana Uyum", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.BolumBazliOee, "Günlük Veriler: Bölüm Bazlı OEE", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.BolumBazliPersonel, "Günlük Veriler: Bölüm Bazlı Personel", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.ModulTrendi, "Günlük Veriler: Modül Trendi", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.HataNedenleri, "Günlük Veriler: Hata Nedenleri", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.BolumBazliHata, "Günlük Veriler: Bölüm Bazlı Hata", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.HataTrendi, "Günlük Veriler: Hata Trendi", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.EnCokHataNedeni, "Günlük Veriler: En Çok Hata Nedeni", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.EnCokHataOperatoru, "Günlük Veriler: En Çok Hata Operatörü", "Günlük Veriler Kartları"),
            new(GunlukVerilerWidgets.EnCokHataBolumu, "Günlük Veriler: En Çok Hata Bölümü", "Günlük Veriler Kartları"),

            new(FikirAtolyesi.View, "Fikir Atölyesi", "Öneri Sistemi"),
            new(Oneri.Create, "Yeni Öneri Oluştur", "Öneri Sistemi"),
            new(Oneri.Query, "Öneri Sorgula", "Öneri Sistemi"),
            new(Oneri.Evaluate, "Öneri Değerlendir (Puanlama)", "Öneri Sistemi"),

            new(OneriAdmin.Access, "Öneri Yönetim Paneli", "Öneri Yönetim Paneli"),
            new(OneriAdmin.Detail, "Öneri Detay Görüntüle", "Öneri Yönetim Paneli"),
            new(OneriAdmin.Approve, "Öneri Onayla", "Öneri Yönetim Paneli"),
            new(OneriAdmin.Reject, "Öneri Reddet", "Öneri Yönetim Paneli"),
            new(OneriAdmin.Delete, "Öneri Sil", "Öneri Yönetim Paneli"),

            new(BolumYoneticileri.View, "Bölüm Yöneticileri Görüntüle", "Bölüm Yöneticileri"),
            new(BolumYoneticileri.Create, "Bölüm Yöneticisi Ekle", "Bölüm Yöneticileri"),
            new(BolumYoneticileri.Delete, "Bölüm Yöneticisi Sil", "Bölüm Yöneticileri"),

            new(VeriYukle.Create, "Excel Veri Yükle", "Veri Yükleme"),
        };
    }
}
