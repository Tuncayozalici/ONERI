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
            new(Dashboards.ProfilLazer, "Profil Lazer Bölümü", "Dashboards"),
            new(Dashboards.Boyahane, "Boyahane Bölümü", "Dashboards"),
            new(Dashboards.Pvc, "PVC Bölümü", "Dashboards"),
            new(Dashboards.Cnc, "CNC Bölümü", "Dashboards"),
            new(Dashboards.Masterwood, "Masterwood Makinesi", "Dashboards"),
            new(Dashboards.Skipper, "Skipper Makinesi", "Dashboards"),
            new(Dashboards.RoverB, "Rover-B Makinesi", "Dashboards"),
            new(Dashboards.Tezgah, "Tezgah Bölümü", "Dashboards"),
            new(Dashboards.Ebatlama, "Ebatlama Bölümü", "Dashboards"),
            new(Dashboards.HataliParca, "Hatalı Parça Analizi", "Dashboards"),

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
