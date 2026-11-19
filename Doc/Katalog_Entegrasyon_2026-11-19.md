# Güncel katalog entegrasyonu — 19 Kasım 2026

Bu dal veri, kaynak kanıtları ve belgeleri içerir. Uygulama kodu değiştirilmedi. Temel sürüm `feature/democontrol @ 5b79729`'dur. Ortak dala birleştirme yapılmadı.

## Dosyalar ve aktarım

- Esas dosya `DataBase/Imports/catalog.ready.json`, biçim schemaVersion 2.
- Excel: `DataBase/Sources/catalogue-2026-11-19/Katalog_Guncel_2026-11-19.xlsx` (19 sayfa). CSV, veri sözlüğü ve kaynak kanıtları aynı klasördedir.
- `DataBase/Imports/2026-11-19/` düzeltme ve kontrol SQL'lerini içerir; geri alma dosyası da bulunduğundan tümünü rastgele çalıştırmayın.
- Eski `catalog.ready.insert-only.sql` giriş noktası durduruldu; 92 programlı eski SQL `history/` altında korunur. Yeni tam SQL denemesi 120 saniyelik süre sınırında geri alındı. Doğrulanan aktarım yolu mevcut uygulama komutudur.

Geliştirme ortamında, özel bağlantı ayarı doğru hedefi gösterirken:

```powershell
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- validate-catalog DataBase/Imports/catalog.ready.json
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- import-catalog DataBase/Imports/catalog.ready.json --apply
```

Doğrulama veritabanına yazmaz. Aktarımdan önce hedef, yedek, migration ve mevcut alan farklarını kontrol edin. Aktarıcı mevcut farklı değerleri otomatik güncellemez.

## Ana yerel veritabanı

Hedef `UniversityDecisionHub`, port 5432. Özel yedek alındı; arşiv listesi okunabildi. Yedek ve bağlantı bilgileri GitHub'a eklenmedi.

| Veri | Kayıt |
|---|---:|
| Fakülte/birim kökü | 9 |
| Bölüm/akademik birim | 135 |
| Öğretim elemanı | 456 |
| Program | 104 |
| Ders | 9.346 |
| Müfredat bağlantısı | 12.591 |

Toplam 22.641 kayıt. Son 20 kredi düzeltmesi uygulandı. Tam alan karşılaştırması sıfır fark; dokuz tekrar kontrolü sıfır grup verdi. Kredi düzeltmesi sırasında hesap ve yorumların bütün satır parmak izleri korundu. Tekrar aktarım `0 added, 22641 unchanged` verdi.

Mevcut `20261116191117_AddReviewModeration` migration'ı uygulandı ve üç sütunu doğrulandı; yeni migration üretilmedi.

## Ayrı geçici PostgreSQL'de kullanıcı akışı

Ana veritabanından yalnızca şema, katalog ve migration geçmişi alındı; gerçek hesap/yorum kopyalanmadı. Üç kurgu hesap ve bir kurgu yorum kullanıldı.

- Tarayıcıda kayıt → profil → Computer Engineering programı → kaydet → yenile → yorum formu çalıştı.
- Formda 175 farklı ders geldi; COME 214 için açıkça kurgu olarak işaretlenmiş yorum kaydedildi.
- Farklı okuyucu hesabıyla giriş yapılıp yorum okundu.
- Test API'si durdurulup yeniden başlatıldı. Yorum farklı hesapla tarayıcıda yeniden okundu; PostgreSQL satırı ayrıca doğrulandı.
- 104 programın kimliği doğrulanmış `/api/programmes/{id}/courses` yanıtları kaynak katalogla alan bazında karşılaştırıldı; 12.591 müfredat bağlantısı eşleşti. Bu, 104 programın hepsinde tarayıcıdan yorum gönderildiği anlamına gelmez.
- Aynı kullanıcı/derse ikinci yorum 409; girişsiz okuma/silme 401; normal yazarın silmesi 403 verdi.
- Kurgu mezun hesapta `isGraduate=true`, sınıf boş korundu. Okuma çalıştı; yorum yazma 403 ile engellendi.
- Ana hesap/yorumların test öncesi/sonrası bütün satır parmak izleri aynı kaldı. Test API'leri kapatıldı ve geçici veritabanı silindi.

Test imza anahtarı yalnızca geçici çalışma ortamındaydı. Ana yerelde sabit `Jwt:Key` yoksa yeniden başlatmada oturumlar düşebilir. Anahtarlar repoya eklenmemelidir.

Kanıtlar: `Doc/Verification/catalogue-2026-11-19/`. Yönetici silme/gizleme bu turda yeniden sınanmadı; önceki ekip testlerinden ayrı tutulur.

## Ortak demoda ve anket tarafında bekleyenler

Ortak demo bilgisayarı, kesin dal/commit ve hedef veritabanı henüz teyit edilmedi. GitHub'dan çekmek başka bilgisayarın veritabanını güncellemez. Hedef belirlenince yedek, migration ve tam alan fark kontrolü yapılmalıdır. Eski 92 programlı hedefte geçmiş 62 alan ve kadro düzeltmeleri ayrıca incelenmeli; anahtar değiştirerek veya silip yeniden ekleyerek geçilmemelidir.

Gerçek anket hesapları aktarılmadı. Mevcut dört sütunlu kullanıcı aktarıcısı ve parolasız hesabı sahiplenmeyi kapatan kayıt davranışı nedeniyle doğrulama, program/mezun alanları ve parola tamamlama sözleşmesi teknik ekipçe netleştirilmelidir. Ayrıntılar `Anket_Aktarimi_Bekleyen_Kararlar_2026-11-19.md` dosyasındadır.

Bilinmeyen kaynak değerleri boş kalır. GİT 1002 kullanıcının isteğiyle ertelendi. 84 programın plan kanıtları güncel derlemede eşleşir; kalan tarihsel programlar yeniden araştırılmış gibi sunulmaz. Profil/kayıt etiketleri ve çoklu kadro gösterimi için arkadaşların koduna yama taşınmadı.
