# GİT 1002: kullanıcı onaylı geçici yerel kodlar

19 Kasım 2026, Europe/Istanbul. Kullanıcı iki ayrı dersi geçici olarak `-0` / `-1` ile ayırmayı onayladı. Bu, üniversitenin resmî kod düzeltmesi veya ekipçe onaylanmış yeni şema değildir.

| Ders | Resmî kaynak kodu | Uygulama/aktarım kodu | Bologna ders kimliği |
|---|---|---|---|
| Yaratıcı Fotoğraf | GİT 1002 | GİT 1002-0 | 4404534 |
| Reklamcılık Tarihi | GİT 1002 | GİT 1002-1 | 4404573 |

Her ikisi DEP-0076 / PRG-0031 kapsamındadır. Kaynaklar `DataBase/Sources/catalogue-2026-11-19/Kaynak_Kanitlari/Ders_Mufredat_Kaynak_Eslesmeleri.csv` içinde tutulur. Kaynak sütunundaki `GİT 1002` değiştirilmedi; yerel kod ayrı `Belgeler/GIT1002_Yerel_Kod_Eslesmesi.csv` eşleştirmesinde kayıtlıdır. Eski bekleyen kaynak satırları tarihsel kanıt olarak korunur.

## Hazırlanan değişiklik

- Eski Yaratıcı Fotoğraf CourseKey'i ve bütün ilişkileri korunarak yalnızca CourseCode `GİT 1002-0` yapıldı.
- Kaynak kimliğine dayanan sabit anahtarla bir Reklamcılık Tarihi dersi eklendi. Geçici koddan kimlik üretilmedi.
- 2026 müfredatındaki 4. ve 8. yarıyıl için iki bağlantı eklendi: seçmeli, 5 AKTS, kaynakta bilinmeyen kredi ve önerilen sınıf boş.
- İki güncel JSON kopyası, Dersler/Mufredat CSV dosyaları, kaynak eşleştirmesi ve açık konu listesi güncellendi.
- Yeni paket: 9 fakülte, 135 bölüm, 456 öğretim elemanı, 104 program, **9.347 ders, 12.593 müfredat bağlantısı; toplam 22.644 katalog kaydı**.
- Kod, arayüz, DTO, şema ve migration değiştirilmedi. Güncel arayüz CourseCode'u kullanırsa yerel ekler görünür; resmî kodu ayrıca gösteren yeni bir ekran geliştirilmedi.

## Kontroller ve uygulama durumu

Gerçek derlenmiş API ile `validate-catalog DataBase/Imports/catalog.ready.json` başarılı: 9/135/456/104/9347/12593. Komut veritabanına bağlanmadığını ve yazmadığını bildirdi. Dönüşümde önceki kayıtların/anahtarların korunması ve yalnızca izin verilen ders kodunun değişmesi denetlendi; bölüm/kod benzersizliği kontrol edildi. Tekrar çalıştırma aynı paketi üretir. Ayrıntılar `Doc/Verification/git1002-local-aliases-2026-11-19/File_Change_Report.json` içindedir.

**19 Kasım 2026, 18:12 +03:00: bu değişiklik yerel UniversityDecisionHub:5432 PostgreSQL veritabanına kullanıcı yetkisiyle uygulandı.** Hedef doğrulandı, özel yedek alındı ve arşiv listesi okunabildi. Mevcut migration geçmişinde AddReviewModeration bulundu; yeni migration oluşturulmadı veya uygulanmadı. Dar kapsamlı ders kodu düzeltmesinden sonra normal importer `3 added, 22641 unchanged` verdi. Tam alan karşılaştırması sıfır fark, dokuz tekrar kontrolü sıfır grup verdi. İkinci aktarım `0 added, 22644 unchanged` verdi. Yeni iki müfredat bağlantısı doğru program/yıl/yarıyıl ve AKTS ile PostgreSQL'den okundu. Tüm public tablolardaki önceki satırların sayım ve tam satır parmak izleri, yalnızca izin verilen eski ders kodu alanı hariç, değişmedi; kullanıcılar ve yorumlar korundu. Kanıtlar aynı doğrulama klasöründedir. HTTP/tarayıcı kontrolü yapılmadı; başka bilgisayardaki ortak demo doğrulanmadı. GitHub yayını ayrı Git kaydıyla izlenir.

Mevcut importer farklı CourseCode'u kendiliğinden güncellemez. `DataBase/Imports/2026-11-19/GIT1002_Yerel_Kodu_Uygula.sql` yerel hedefte yedekten sonra çalıştırıldı; kimlik ve ilişkiler korunarak yalnızca beklenen kod düzeltildi. Diğer bilgisayarlarda hedef/yedek kontrolünden sonra aynı sıra gerekir: dar kapsamlı kod düzeltmesi, normal katalog aktarımı ve alan/ilişki/idempotence kontrolü. Tamamen yeni hedefte doğrudan güncel paket kullanılabilir. GitHub'dan çekmek bu işlemleri kendiliğinden yapmaz.

## Belge ve Excel sınırlaması

`Katalog_Guncel_2026-11-19.xlsx` ve önceki doğrulama raporları bu değişiklikten önceki sürümü gösterir. Excel güncellenmedi. Güncel aktarım için JSON/CSV ve bu karar esas alınmalıdır. Spreadsheets/Documents becerilerinin istediği `load_workspace_dependencies` aracı oturumda bulunmadığından Excel düzenlemesi, `Doc/Implementation_Log.docx` güncellemesi ve bunların görsel doğrulaması tamamlanamadı. Word geçmişi korunmuştur; aşağıdaki kayıt Word günlüğüne aktarılmayı bekler.

## Bekleyen implementation log kaydı

Date: 19 November 2026 (Europe/Istanbul), application verified at 18:12 +03:00. Request: apply user-approved temporary local aliases GİT 1002-0 / GİT 1002-1, supply a group message draft, then apply to local PostgreSQL and push. Updated both catalogue JSON copies, course/curriculum CSVs, source mapping and issue status; added a reproducible scoped conversion script, alias mapping, verification reports and a guarded SQL correction. Verified private backup and existing migrations. Preserved the old CourseKey and all prior relationships; corrected only its CourseCode, then imported one source-identified course and two curriculum occurrences. No endpoints/DTO/schema changes and no generated/applied migration. File-only validation passed with 9/135/456/104/9347/12593. Real local PostgreSQL import: 3 added, 22641 unchanged; full field comparison: zero differences; nine duplicate checks: zero groups; repeat import: 0 added, 22644 unchanged. All pre-existing public-table rows preserved except the authorized course code; accounts and reviews unchanged. Initial preflight attempts stopped before writes due to Windows script encoding and native command quoting; corrected before application. HTTP/browser verification and another machine's demo remain untested. User authorized Git commit/push; publication is tracked by Git. No team message sent. Existing workbook and Word log remain unchanged due to unavailable mandated artifact runtime; visual document review is pending. Official code confirmation remains open; suffixes are temporary local aliases.

## Gruba gönderilecek metin

Arkadaşlar, resmî kaynakta Yaratıcı Fotoğraf ve Reklamcılık Tarihi aynı GİT 1002 koduyla göründüğü için, mevcut bölüm/ders kodu benzersizlik kuralına takılıyorlardı. Şimdilik Yaratıcı Fotoğrafı **GİT 1002-0**, Reklamcılık Tarihini **GİT 1002-1** olarak ayırdık. Bunlar bizim geçici yerel kodlarımız; resmî kod kaynak kayıtlarında GİT 1002 olarak korunuyor. Yerel PostgreSQL'e uygulandı: bir ders ve iki müfredat bağlantısı eklendi; eski dersin kimliği, kullanıcılar ve yorumlar korundu. Tam alan karşılaştırması ve tekrar aktarım kontrolü geçti. Kendi veritabanınız için çekmek yeterli değil; hedef/yedek kontrolü, mevcut ders kodunun dar kapsamlı düzeltmesi ve katalog aktarımı gerekiyor. Uygulama sırası karar notunda var. Resmî kod teyidi gelince eşleştirmeyi tekrar değerlendireceğiz.
