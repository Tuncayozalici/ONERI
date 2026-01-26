namespace ONERI.Models;

public enum OneriDurum
{
    Reddedildi = -1,    // Bölüm Yöneticisi Reddeti
    Beklemede = 0,      // İlk Oluşturuldu, Yönetici Onayı Bekliyor
    Onaylandi = 1,      // Bölüm Yöneticisi Onayladı, Kurul Değerlendirmesi Bekliyor
    KabulEdildi = 2,    // Kurul Değerlendirdi ve 60 Puan Üstü Aldı
    PuanlamaRed = 3     // Kurul Değerlendirdi ve 60 Puan Altı Aldı
}
