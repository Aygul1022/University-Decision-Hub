# MYO kaynak paketi aktarım öncesi sonucu

17 Kasım 2026. Bu rapor dosya hazırlama ve doğrulama sonucudur. PostgreSQL'e aktarım yapılmadı.

## Girdiler

- `DataBase/Sources/myo-bologna-2026-11-17`: yedi özgün kaynak dosyası.
- `DataBase/Imports/catalog.ready.json`: 92 program, 303 ders, 316 müfredat satırı.
- Kullanıcının hedef PostgreSQL'den aldığı `curriculum-key-check.csv`: 316 mevcut müfredat satırı; temel JSON ile alan alan karşılaştırıldı. NULL metinleri boş değer olarak okundu.

## Çıktılar

- `private-data/myo-import-2026-11-17/catalog.myo.candidate.json`: mevcut kayıtları koruyan schemaVersion=2 aday dosya.
- `private-data/myo-import-2026-11-17/myo-import-report.json`: her kaynak satırının durumu, kaynak bağlantısı, SHA256 değerleri ve program bazında kapsam. Aktarım JSON'u değildir.

| Ölçüt | Sonuç |
|---|---:|
| İncelenen ders–yarıyıl satırı | 3595 |
| Yeni müfredat satırı | 3415 |
| Korunan mevcut Anestezi satırı | 5 |
| Program anahtarı bekleyen satır | 175 |
| Yeni ders | 3089 |
| Adaydaki toplam ders | 3392 |
| Adaydaki toplam müfredat satırı | 3731 |
| Ders bağlantısı olan program, önce / sonra | 3 / 47 |

Ders sayısı benzersiz bölüm/ders-kodu kimliğine dayanır; Bologna kartı ve müfredat satırı sayısıyla aynı değildir.

## Korunan veriler ve bekleyen işler

Tüm temel katalog satırları, anahtarları ve değerleri değişmeden korundu. Mevcut beş Anestezi satırının yeni kaynakta gelen yıl/dönem/seçmelilik ve ad farkları rapora yazıldı; otomatik güncellenmedi. Yeni satırlarda 1612 boş kredi ve iki bilinmeyen pozitif yarıyıl null kaldı. Önerilen sınıf yarıyıldan tahmin edilmedi.

İş Sağlığı ve Güvenliği ve Sosyal Hizmetler programları için kaynak anahtarı olmadığından 175 satır beklemede. Anahtarları ve bölüm ilişkileri Ümran tarafından netleştirilmeden eklenmez. 299 seçmeli grup başlığı Course olarak eklenmedi. Öğretim elemanı adı olmayan kartlar ana kaynak satırlarına tekrar eklenmedi. Kart öğretim elemanlarından CourseOffering/OfferingInstructor üretilmedi.

Bu paket MYO kapsamındadır. Bilgisayar Mühendisliği müfredatını içermez. Diğer fakülteler için doğrulanmış kaynak gerekir; tüm bölümlerin ders sahibi olduğu iddia edilmez. Program aday anahtarları ve adları temel katalogla karşılaştırıldı; bu otomatik kontrol kaynak sahibinin kimlik onayının yerine geçmez.

## Doğrulama

- Uygulamanın derlenmiş DLL'i ile `validate-catalog`: 9 fakülte, 130 bölüm, 452 öğretim elemanı, 92 program, 3392 ders, 3731 müfredat satırı; başarılı. Veritabanı bağlantısı veya yazısı yok.
- 16 dosya kontrolü: tüm 3595 satırın muhasebesi, temel kayıtların korunması, benzersiz kimlikler, null değerler, yeniden dönüşümde tekrar oluşmaması ve kaynak sırası değişince kimliklerin korunması.
- Aday çıktı gitignored `private-data` içindedir. Mevcut JSON ve kaynak dosyalar üzerine yazılmadı.

## Kullanıcı tarafından uygulanacak sıra

1. Kaynak sahibi program eşleştirmeleri ve bekleyen raporu inceler.
2. Doğru hedef PostgreSQL ve yedek doğrulanır; güncel veri temel JSON'dan farklıysa importer çakışmayı reddedebilir.
3. Kullanıcı dosyayı yeniden `validate-catalog` ile doğrular.
4. Kullanıcı ayrıca karar verdiğinde mevcut `import-catalog ... --apply` komutunu çalıştırır. Bu hazırlık bu komutu çalıştırmadı.
5. Eklenen/atlanan sayılar, tekrar aktarım ve profil → ders listesi uygulama kontrolleri kaydedilir. Yorum gönderimi ayrı testtir.
