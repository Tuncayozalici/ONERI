# ONERI

ASP.NET Core MVC tabanli fabrika onerileri, yetkilendirme ve uretim dashboard uygulamasi.

## Gereksinimler

- .NET SDK 8.0
- EF Core CLI araci
- SQLite destegi
- EPPlus lisans kosullarinin proje kullanimi icin uygun oldugunun dogrulanmasi

Bu makinede kontrol edilen SDK surumu:

```bash
dotnet --version
```

Beklenen ana surum: `8.x`

EF Core CLI yoksa yukleyin:

```bash
dotnet tool install --global dotnet-ef
```

## Kurulum

Bagimliliklari geri yukleyin:

```bash
dotnet restore
```

Veritabanini migration'larla olusturun veya guncelleyin:

```bash
dotnet ef database update
```

Uygulamayi calistirin:

```bash
dotnet run
```

Development profilinde varsayilan adresler `Properties/launchSettings.json` icinden okunur.

## Veritabani

Varsayilan baglanti `appsettings.json` icindedir:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=fabrika.db"
}
```

Mevcut kurulum SQLite kullanir. ERP entegrasyonunda farkli bir veritabani provider'i kullanilacaksa sadece connection string degil, EF provider paketi, migration SQL'leri, raw SQL kullanimlari ve cache tablo olusturma davranisi da gozden gecirilmelidir.

Dashboard verisi su anda `wwwroot/EXCELS` altindaki Excel dosyalarindan okunur ve `DashboardCacheEntries` isimli cache tablosuna JSON snapshot olarak yazilir. Bu tablo servis icinde raw SQL ile olusturulur; migration modeline dahil degildir.

## Migration Komutlari

Yeni migration eklemek icin:

```bash
dotnet ef migrations add MigrationAdi
```

Migration'lari uygulamak icin:

```bash
dotnet ef database update
```

Migration durumunu gormek icin:

```bash
dotnet ef migrations list
```

## Test

Tum testleri calistirin:

```bash
dotnet test
```

Son kontrol sonucu: 22 test basarili.

## Ilk Admin Kullanici

Uygulama baslangicinda `DbSeeder` roller ve varsayilan izinleri olusturur. Ilk Super Admin kullanicisi icin sifre config veya env degiskeninden gelmelidir.

Desteklenen ayarlar:

- `BootstrapAdmin:UserName`
- `BootstrapAdmin:Email`
- `BootstrapAdmin:FullName`
- `BootstrapAdmin:Password`
- `BOOTSTRAP_ADMIN_PASSWORD`

Ornek user-secrets kurulumu:

```bash
dotnet user-secrets set "BootstrapAdmin:UserName" "admin"
dotnet user-secrets set "BootstrapAdmin:Email" "admin@marwood.com"
dotnet user-secrets set "BootstrapAdmin:FullName" "Admin"
dotnet user-secrets set "BootstrapAdmin:Password" "Degistirilmeli-123!"
```

Alternatif env degiskeni:

```bash
export BOOTSTRAP_ADMIN_PASSWORD="Degistirilmeli-123!"
```

Sifre tanimli degilse uygulama Super Admin kullanicisini olusturmaz ve log yazar.

## Ortam Degiskenleri ve Config

Kullanilan baslica config/env anahtarlari:

- `ASPNETCORE_ENVIRONMENT`: `Development`, `Staging`, `Production`
- `ConnectionStrings__DefaultConnection`: veritabani baglantisi
- `BootstrapAdmin__UserName`: ilk admin kullanici adi
- `BootstrapAdmin__Email`: ilk admin e-posta
- `BootstrapAdmin__FullName`: ilk admin ad soyad
- `BootstrapAdmin__Password`: ilk admin sifresi
- `BOOTSTRAP_ADMIN_PASSWORD`: ilk admin sifresi icin alternatif env degiskeni
- `DOTNET_EF_DESIGN_TIME`: EF design-time calisma ayrimi icin kullanilir

Linux/macOS ornegi:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Data Source=fabrika.db"
export BOOTSTRAP_ADMIN_PASSWORD="Degistirilmeli-123!"
```

## ERP Entegrasyonu Icin Notlar

- Dashboard veri akisi Excel tabanlidir; ERP entegrasyonunda Excel parser yerine yeni veri kaynagi katmani tasarlanmalidir.
- `IDashboardIngestionService` ve `IDashboardQueryService` entegrasyon sinirinin ana adaylaridir.
- Bazi dashboard metrikleri eksik veri durumunda tahmini/turetilmis deger uretebilir. ERP verisi baglanirken bu alanlar gercek kaynak kolonlarla eslestirilmeli veya ekranda tahmini oldugu acikca belirtilmelidir.
- `wwwroot/EXCELS` altindaki dosyalar ornek/veri kaynagi olarak repo icindedir. Canli ERP entegrasyonunda kalici kaynak olarak kullanilmamalidir.
- Upload ekrani dosyalari `wwwroot` altina yazar. Uretim ortaminda dosya boyutu, icerik tipi, audit log ve storage politikasi ayrica belirlenmelidir.

## Teslim Oncesi Kontrol

```bash
git status --short
dotnet restore
dotnet ef database update
dotnet test
```

Teslimden once calisma agacinin temiz oldugunu, admin sifresinin secret/env uzerinden verildigini ve ERP icin hedef veritabani kararinin yazili oldugunu kontrol edin.
