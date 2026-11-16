# Ortak katalog kurulumu

## Güncel dönüşüm durumu

16 Kasım güncellemesi: katalog artık 303 ders/316 müfredat kaydı, toplam 1302 kayıt içerir (pilot ve doğrulanmış İşletme/EEM kayıtları). Aşağıdaki eski pilot sayıları yalnız tarihsel başlangıçtır. Yeni dosya için uygulama doğrulaması Windows Application Control nedeniyle tamamlanamadı; import öncesinde validate-catalog çalıştırılmalıdır. Yapısal anahtar/ilişki ve eski kayıtların korunması kontrolleri geçti.

`Sources/course.staging.json` bütün 5442 ham satırı korur: 311 dahil, 5125 bekleyen, 6 ders olmayan grup başlığı. Bu arşiv import-catalog dosyası değildir. Kullanıcı tüm müfredat yıllarını onayladı; EEM dosyasındaki 214 adet 2026 satırı dahil edildi. 5125 satırın program/bölüm eşleştirmesi kaynakta hâlâ adaydır.

Yeni Excel satırlarını dönüştürmek için Python ortamına `Scripts/requirements-catalog.txt` bağımlılıklarını kurup proje kökünde:

```powershell
python Scripts/Convert-CatalogExcelToJson.py --source "C:/Users/aygul/OneDrive/Desktop/Datas2" --output "private-data/catalog.next.json" --staging-report "private-data/staging.next.json" --eem-year 2026
```

Mevcut çıktı üzerine yazılmaz. Kaynak yıl doğrulandıktan sonra yalnız EEM için `--eem-year 2025` veya `--eem-year 2026` verilebilir. Anahtarlar satır sırasından bağımsız üretilir. Ders kimliği bölüm/kod; müfredat kimliği program/ders/yıl/dönemdir. Yıl/dönem düzeltmesi yeni kimlik oluşturabileceğinden mevcut DB için değişiklik ayrıca incelenmelidir. Yeni program eşleştirmeleri kendiliğinden güvenilir sayılmaz; kanıt kuralı eklenmelidir. Kredi T+U+L'den tahmin edilmez.

Ekip için güncel dosya `DataBase/Imports/catalog.ready.json` dosyasıdır. Eski `private-data/catalog.pilot*.json` dosyalarını paylaşmak gerekmez. JSON'un GitHub'a ulaşması için bu yeni dosyaların commit edilip pushlanması gerekir; bu hazırlık sırasında push yapılmadı.

16 Kasım 2026 paketi: 9 fakülte, 130 bölüm, 92 program, 452 öğretim elemanı, 5 Anestezi dersi ve 5 müfredat kaydı (toplam 693). Kaynak ve dönüşüm notları `Sources/README.md` dosyasındadır. Bu klasöre öğrenci listesi, şifre veya bağlantı bilgisi eklemeyin.

## Her ekip üyesinin yerel kurulumu

Güncel kodu ve JSON'u alın. .NET 10 ve yerel PostgreSQL kullanın. `UniversityDecisionHub` veritabanınızı ve özel `ConnectionStrings:DefaultConnection` ayarınızı önceden hazırlayın. Herkes kendi bağlantısını kullanır; örnek bir arkadaşın sayısal ID'lerini kopyalamayın.

Proje kökünde PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef database update --project Backend/UniversityDecisionHub.Api --startup-project Backend/UniversityDecisionHub.Api --context ApplicationDbContext
```

Bu komut bekleyen migration'ları yerel veritabanına uygular. Mevcut veritabanında önce bekleyen migration'ları inceleyin ve gerekli yedeği alın. EF CLI kurulu değilse Visual Studio Package Manager Console'da API projesini Default Project ve Startup Project seçerek `Update-Database -Context ApplicationDbContext` kullanabilirsiniz. Yeni migration oluşturmanız gerekmez.

Önce dosyayı doğrulayın; bu komut veritabanına bağlanmaz:

```powershell
$catalogPath = (Resolve-Path 'DataBase/Imports/catalog.ready.json').Path
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- validate-catalog "$catalogPath"
```

Doğrulama başarılıysa aktarın; aşağıdaki komut yerel veritabanına yazar:

```powershell
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- import-catalog "$catalogPath" --apply
```

Beklenen sonuçlar, mevcut DB durumuna bağlıdır: boş ve migration'ları uygulanmış DB'de 693 ekleme; önceki pilot ve ANS107 düzeltmesi eksiksiz olan DB'de 4 ekleme/689 değişmeyen kayıt; aynı dosya yeniden aktarıldığında 0 ekleme/693 değişmeyen kayıt. Bunlar beklenen değerlerdir, bu hazırlık sırasında gerçek DB'de ölçülmedi.

Importer mevcut farklı değerleri otomatik güncellemez veya kayıt silmez. Çakışma olursa hata metnini paylaşın; tablo silmeyin veya anahtarları yeniden numaralandırmayın. Özellikle eski pilotta ANS107 isElective=null kalmışsa onaylı düzeltme gereklidir.

pgAdmin'de kendi sunucunuz altında UniversityDecisionHub > Schemas > public > Tables listesini yenileyin. Faculties, Departments, Programmes, Instructors, Courses ve ProgrammeCourses tablolarında View/Edit Data > All Rows ile kayıtları görebilirsiniz. CourseOffering/OfferingInstructor için bu paket kayıt içermez.

## Paylaşım

Aygül `DataBase` klasöründeki yeni dosyaları commit/push ettikten sonra ekip aynı dal/commit'i almalıdır. Dosyanın bilgisayarda bulunması veya JSON doğrulamasının geçmesi tek başına DB aktarımı değildir. Ham kaynaklar ve eski özel pilotlar taşınmadı; yalnız paylaşılabilir katalog alanları yeni JSON'a alındı.
