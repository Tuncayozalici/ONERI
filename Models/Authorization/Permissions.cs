using System;
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
            // Eski toplu yetkiler. Yeni rol ekranında tekil kart yetkileri gösterilir,
            // bu anahtarlar mevcut veritabanı claim'lerini yeni yapıya taşımak için tutulur.
            public const string NabizOzeti = "dashboard.gunluk.widget.nabizozeti.view";
            public const string DonemOzetKartlari = "dashboard.gunluk.widget.donemozeti.view";
            public const string UretimAkisi = "dashboard.gunluk.widget.uretimakisi.view";
            public const string AkisPlanlama = "dashboard.gunluk.widget.akisplanlama.view";
            public const string KritikGercekler = "dashboard.gunluk.widget.kritikgercekler.view";
            public const string MakineDurumListesi = "dashboard.gunluk.widget.makinedurum.view";
            public const string ProsesDetayKartlari = "dashboard.gunluk.widget.prosesdetay.view";
            public const string BolumEkranlari = "dashboard.gunluk.widget.bolumekranlari.view";
            public const string SshMusteriGercegi = "dashboard.gunluk.widget.sshmusteri.view";
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
            public const string EnCokHataBolumu = "dashboard.gunluk.widget.encokhatabolumu.view";

            public const string GelenSiparisAdedi = "dashboard.gunluk.card.gelensiparis.view";
            public const string ToplamSiparisModulAdedi = "dashboard.gunluk.card.siparismodul.view";
            public const string SiparisPlanDurumu = "dashboard.gunluk.card.siparisplandurumu.view";
            public const string TicariSiparisAdedi = "dashboard.gunluk.card.ticarisiparis.view";
            public const string KaosEndeksi = "dashboard.gunluk.card.kaosendeksi.view";
            public const string PlanGerceklesme = "dashboard.gunluk.card.plangerceklesme.view";
            public const string TerminUyum = "dashboard.gunluk.card.terminuyum.view";
            public const string KapasiteKullanimi = "dashboard.gunluk.card.kapasitekullanimi.view";
            public const string CalisilanIsGunu = "dashboard.gunluk.card.calisilanisgunu.view";
            public const string OrtalamaCalisanPersonel = "dashboard.gunluk.card.ortalamapersonel.view";
            public const string GunlukUretim = "dashboard.gunluk.card.gunlukuretim.view";
            public const string Wip = "dashboard.gunluk.card.wip.view";
            public const string PlanlananGerceklesen = "dashboard.gunluk.card.planlanangerceklesen.view";
            public const string PlanBozulma = "dashboard.gunluk.card.planbozulma.view";
            public const string AcilIs = "dashboard.gunluk.card.acilis.view";
            public const string PlanBekleyen = "dashboard.gunluk.card.planbekleyen.view";
            public const string MamEtkisi = "dashboard.gunluk.card.mametkisi.view";
            public const string SshHataAdedi = "dashboard.gunluk.card.sshhataadedi.view";
            public const string SshBolumEtkisi = "dashboard.gunluk.card.sshbolumetkisi.view";
            public const string CalisanMakineOrani = "dashboard.gunluk.card.calisanmakine.view";
            public const string DurusAriza = "dashboard.gunluk.card.durusariza.view";
            public const string MakineDurumKayitlari = "dashboard.gunluk.card.makinedurumkayitlari.view";
            public const string DepoyaGirenModul = "dashboard.gunluk.card.depoagirismodul.view";
            public const string EnTrendUrun = "dashboard.gunluk.card.entrendurun.view";

            public const string ProsesDosemeKesim = "dashboard.gunluk.card.proses.dosemekesim.view";
            public const string ProsesDosemeCnc = "dashboard.gunluk.card.proses.dosemecnc.view";
            public const string ProsesSungerKesim = "dashboard.gunluk.card.proses.sungerkesim.view";
            public const string ProsesDosemeDikim = "dashboard.gunluk.card.proses.dosemedikim.view";
            public const string ProsesDosemeIskelet = "dashboard.gunluk.card.proses.dosemeiskelet.view";
            public const string ProsesKanepeBeyazlama = "dashboard.gunluk.card.proses.kanepebeyazlama.view";
            public const string ProsesKanepeDoseme = "dashboard.gunluk.card.proses.kanepedoseme.view";
            public const string ProsesKanepeMontaj = "dashboard.gunluk.card.proses.kanepemontaj.view";
            public const string ProsesMakamBeyazlama = "dashboard.gunluk.card.proses.makambeyazlama.view";
            public const string ProsesMakamDoseme = "dashboard.gunluk.card.proses.makamdoseme.view";
            public const string ProsesMakamMontaj = "dashboard.gunluk.card.proses.makammontaj.view";
            public const string ProsesDosemePaketleme = "dashboard.gunluk.card.proses.dosemepaketleme.view";
            public const string ProsesKesim = "dashboard.gunluk.card.proses.kesim.view";
            public const string ProsesBantlama = "dashboard.gunluk.card.proses.bantlama.view";
            public const string ProsesDelikCnc = "dashboard.gunluk.card.proses.delikcnc.view";
            public const string ProsesKeson = "dashboard.gunluk.card.proses.keson.view";
            public const string ProsesSacLazer = "dashboard.gunluk.card.proses.saclazer.view";
            public const string ProsesProfilKesimEgim = "dashboard.gunluk.card.proses.profilkesimegim.view";
            public const string ProsesKaynakTesviye = "dashboard.gunluk.card.proses.kaynaktesviye.view";
            public const string ProsesBoya = "dashboard.gunluk.card.proses.boya.view";
            public const string ProsesKurulum = "dashboard.gunluk.card.proses.kurulum.view";
            public const string ProsesMontaj = "dashboard.gunluk.card.proses.montaj.view";

            public static readonly IReadOnlyList<string> All = new List<string>
            {
                GelenSiparisAdedi,
                ToplamSiparisModulAdedi,
                SiparisPlanDurumu,
                TicariSiparisAdedi,
                KaosEndeksi,
                PlanGerceklesme,
                TerminUyum,
                KapasiteKullanimi,
                CalisilanIsGunu,
                OrtalamaCalisanPersonel,
                GunlukUretim,
                Wip,
                PlanlananGerceklesen,
                PlanBozulma,
                AcilIs,
                PlanBekleyen,
                MamEtkisi,
                SshHataAdedi,
                SshBolumEtkisi,
                CalisanMakineOrani,
                DurusAriza,
                MakineDurumKayitlari,
                ProsesDosemeKesim,
                ProsesDosemeCnc,
                ProsesSungerKesim,
                ProsesDosemeDikim,
                ProsesDosemeIskelet,
                ProsesKanepeBeyazlama,
                ProsesKanepeDoseme,
                ProsesKanepeMontaj,
                ProsesMakamBeyazlama,
                ProsesMakamDoseme,
                ProsesMakamMontaj,
                ProsesDosemePaketleme,
                ProsesKesim,
                ProsesBantlama,
                ProsesDelikCnc,
                ProsesKeson,
                ProsesSacLazer,
                ProsesProfilKesimEgim,
                ProsesKaynakTesviye,
                ProsesBoya,
                ProsesKurulum,
                ProsesMontaj,
                DepoyaGirenModul,
                EnTrendUrun,
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
                EnCokHataBolumu,
                HataliAdet
            };

            public static readonly IReadOnlyList<string> LegacyAll = new List<string>
            {
                NabizOzeti,
                DonemOzetKartlari,
                UretimAkisi,
                AkisPlanlama,
                KritikGercekler,
                MakineDurumListesi,
                ProsesDetayKartlari,
                BolumEkranlari,
                SshMusteriGercegi,
                ToplamModulSayisi
            };

            public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LegacyExpansionMap =
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                {
                    [NabizOzeti] = new[]
                    {
                        KaosEndeksi,
                        PlanGerceklesme,
                        TerminUyum,
                        KapasiteKullanimi
                    },
                    [DonemOzetKartlari] = new[]
                    {
                        CalisilanIsGunu,
                        OrtalamaCalisanPersonel
                    },
                    [UretimAkisi] = new[]
                    {
                        GunlukUretim,
                        Wip,
                        PlanlananGerceklesen
                    },
                    [AkisPlanlama] = new[]
                    {
                        PlanBozulma,
                        AcilIs,
                        PlanBekleyen,
                        MamEtkisi
                    },
                    [MakineDurumListesi] = new[]
                    {
                        CalisanMakineOrani,
                        DurusAriza,
                        MakineDurumKayitlari
                    },
                    [ProsesDetayKartlari] = new[]
                    {
                        ProsesDosemeKesim,
                        ProsesDosemeCnc,
                        ProsesSungerKesim,
                        ProsesDosemeDikim,
                        ProsesDosemeIskelet,
                        ProsesKanepeBeyazlama,
                        ProsesKanepeDoseme,
                        ProsesKanepeMontaj,
                        ProsesMakamBeyazlama,
                        ProsesMakamDoseme,
                        ProsesMakamMontaj,
                        ProsesDosemePaketleme,
                        ProsesKesim,
                        ProsesBantlama,
                        ProsesDelikCnc,
                        ProsesKeson,
                        ProsesSacLazer,
                        ProsesProfilKesimEgim,
                        ProsesKaynakTesviye,
                        ProsesBoya,
                        ProsesKurulum,
                        ProsesMontaj
                    },
                    [SshMusteriGercegi] = new[]
                    {
                        SshHataAdedi,
                        SshBolumEtkisi
                    },
                    [ToplamModulSayisi] = new[]
                    {
                        DepoyaGirenModul,
                        EnTrendUrun
                    }
                };

            public static IReadOnlyCollection<string> ExpandLegacyPermission(string permissionKey)
            {
                return LegacyExpansionMap.TryGetValue(permissionKey, out var expandedPermissions)
                    ? expandedPermissions
                    : Array.Empty<string>();
            }
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

            new(GunlukVerilerWidgets.GelenSiparisAdedi, "Gelen Sipariş Adedi", "Günlük Veriler - Sipariş Kartları"),
            new(GunlukVerilerWidgets.ToplamSiparisModulAdedi, "Toplam Modül Adedi", "Günlük Veriler - Sipariş Kartları"),
            new(GunlukVerilerWidgets.SiparisPlanDurumu, "Planlanan / Planlanmayan", "Günlük Veriler - Sipariş Kartları"),
            new(GunlukVerilerWidgets.TicariSiparisAdedi, "Ticari Sipariş Adedi", "Günlük Veriler - Sipariş Kartları"),

            new(GunlukVerilerWidgets.KaosEndeksi, "Kaos Endeksi", "Günlük Veriler - Fabrika Özeti"),
            new(GunlukVerilerWidgets.PlanGerceklesme, "Plan Gerçekleşme", "Günlük Veriler - Fabrika Özeti"),
            new(GunlukVerilerWidgets.TerminUyum, "Termin Uyum", "Günlük Veriler - Fabrika Özeti"),
            new(GunlukVerilerWidgets.KapasiteKullanimi, "Kapasite Kullanımı", "Günlük Veriler - Fabrika Özeti"),
            new(GunlukVerilerWidgets.CalisilanIsGunu, "Çalışılan İş Günü", "Günlük Veriler - Fabrika Özeti"),
            new(GunlukVerilerWidgets.OrtalamaCalisanPersonel, "Ortalama Çalışan Personel", "Günlük Veriler - Fabrika Özeti"),

            new(GunlukVerilerWidgets.GunlukUretim, "Günlük Üretim", "Günlük Veriler - Üretim Akışı"),
            new(GunlukVerilerWidgets.Wip, "WIP", "Günlük Veriler - Üretim Akışı"),
            new(GunlukVerilerWidgets.PlanlananGerceklesen, "Planlanan vs Gerçekleşen", "Günlük Veriler - Üretim Akışı"),

            new(GunlukVerilerWidgets.PlanBozulma, "Plan Bozulma", "Günlük Veriler - Akış Planlama"),
            new(GunlukVerilerWidgets.AcilIs, "Acil İş", "Günlük Veriler - Akış Planlama"),
            new(GunlukVerilerWidgets.PlanBekleyen, "Plan Bekleyen", "Günlük Veriler - Akış Planlama"),
            new(GunlukVerilerWidgets.MamEtkisi, "MAM Etkisi", "Günlük Veriler - Akış Planlama"),

            new(GunlukVerilerWidgets.SshHataAdedi, "SSH Hata Adedi", "Günlük Veriler - SSH ve Makine"),
            new(GunlukVerilerWidgets.SshBolumEtkisi, "SSH Bölüm Etkisi", "Günlük Veriler - SSH ve Makine"),
            new(GunlukVerilerWidgets.CalisanMakineOrani, "Çalışan Makine", "Günlük Veriler - SSH ve Makine"),
            new(GunlukVerilerWidgets.DurusAriza, "Duruş / Arıza", "Günlük Veriler - SSH ve Makine"),
            new(GunlukVerilerWidgets.MakineDurumKayitlari, "Makine Durum Kayıtları", "Günlük Veriler - SSH ve Makine"),

            new(GunlukVerilerWidgets.ProsesDosemeKesim, "Proses: Döşeme Kesim", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesDosemeCnc, "Proses: Döşeme CNC", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesSungerKesim, "Proses: Sünger Kesim", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesDosemeDikim, "Proses: Döşeme Dikim", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesDosemeIskelet, "Proses: Döşeme İskelet", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKanepeBeyazlama, "Proses: Kanepe Beyazlama", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKanepeDoseme, "Proses: Kanepe Döşeme", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKanepeMontaj, "Proses: Kanepe Montaj", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesMakamBeyazlama, "Proses: Makam Beyazlama", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesMakamDoseme, "Proses: Makam Döşeme", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesMakamMontaj, "Proses: Makam Montaj", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesDosemePaketleme, "Proses: Döşeme Paketleme", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKesim, "Proses: Kesim", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesBantlama, "Proses: Bantlama", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesDelikCnc, "Proses: Delik - CNC", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKeson, "Proses: Keson", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesSacLazer, "Proses: Saç Lazer", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesProfilKesimEgim, "Proses: Profil Kesim Eğim", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKaynakTesviye, "Proses: Kaynak Tesviye", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesBoya, "Proses: Boya", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesKurulum, "Proses: Kurulum", "Günlük Veriler - Proses Kartları"),
            new(GunlukVerilerWidgets.ProsesMontaj, "Proses: Montaj", "Günlük Veriler - Proses Kartları"),

            new(GunlukVerilerWidgets.DepoyaGirenModul, "Depoya Giren Modül", "Günlük Veriler - KPI Kartları"),
            new(GunlukVerilerWidgets.EnTrendUrun, "En Trend Ürün", "Günlük Veriler - KPI Kartları"),
            new(GunlukVerilerWidgets.ToplamDuraklama, "Toplam Duraklama", "Günlük Veriler - KPI Kartları"),
            new(GunlukVerilerWidgets.EnCokHataNedeni, "En Çok Hata Nedeni", "Günlük Veriler - KPI Kartları"),
            new(GunlukVerilerWidgets.EnCokHataBolumu, "En Çok Hata Bölümü", "Günlük Veriler - KPI Kartları"),
            new(GunlukVerilerWidgets.HataliAdet, "Hatalı Adet", "Günlük Veriler - KPI Kartları"),

            new(GunlukVerilerWidgets.GenelOeeSkoru, "Genel OEE Skoru", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.MakineBazliOee, "Makine Bazlı OEE", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.OeeBilesenleri, "OEE Bileşenleri", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.BolumBazliPlanaUyum, "Bölüm Bazlı Plana Uyum", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.BolumBazliOee, "Bölüm Bazlı OEE", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.BolumBazliPersonel, "Bölüm Bazlı Personel", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.ModulTrendi, "Modül Trendi", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.HataNedenleri, "Hata Nedenleri", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.BolumBazliHata, "Bölüm Bazlı Hata", "Günlük Veriler - Grafik Kartları"),
            new(GunlukVerilerWidgets.HataTrendi, "İstasyon Doluluk Karşılaştırması", "Günlük Veriler - Grafik Kartları"),

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
