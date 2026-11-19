# Güncel proje veri teslimi

**Sürüm: 19 Kasım 2026 — 104 programlı katalog.** Bu paket veri hazırlığı ve inceleme içindir. Bu turda uygulama kodu, PostgreSQL, gerçek kullanıcı/yorum, migration veya GitHub değişikliği yapılmadı. Entegrasyon bu teslimin kontrolünden sonra ayrı adım olarak yapılacak.

## Kullanılacak dosyalar

1. `Katalog_Guncel_2026-11-19.xlsx`: filtrelenebilir ana Excel; tüm katalog tabloları, kaynak eşleştirmeleri, ders kartları, kadro kayıtları, anonim anket eşleştirmesi, veri sözlüğü, açık konular ve eski dosya envanteri.
2. `Veri/catalog.ready.json`: güncel uygulamanın v2 aktarım sözleşmesine uygun tam katalog. Eski GitHub dosyasındaki 92 programlık örnekle karıştırılmamalı.
3. `Veri/*.csv`: JSON'un altı katalog listesinin UTF-8 CSV karşılıkları ve anonim anket eşleştirmesi. Anket dosyası kullanıcı aktarım biçimi değildir.
4. `Kaynak_Kanitlari/`: 10.749 kaynak satırı, 8.401 ders kartı kanıtı, 485 çoklu kadro üyeliği, 36 kayıt kararı, 82 geçmiş bölüm düzeltmesi ve bekleyen GİT 1002 satırları.
5. `Kontroller/`: kontrol sorguları, önceki gerçek PostgreSQL kontrol sonuçları ve bu teslimin yeni dosya/Excel/JSON kontrol sonuçları. Önceki sonuçların yeniden çalıştırılmış gibi tarihleri değiştirilmez.
6. `Belgeler/`: güncel 11 tablo veri sözlüğü, ilişki modeli, açık konular/sorumlular ve entegrasyon adımları.
7. `Ekip_Onayi/`: güncel GitHub inceleme notu ve gönderilmeye hazır revize ekip mesajı. Önceki profile.js yaması yeni ekip dosyasının üstüne kopyalanmaz.

## Sayılar ve uygulama durumu

| Veri | Kayıt |
|---|---:|
| Fakülte/birim kökü | 9 |
| Bölüm/akademik birim | 135 |
| Öğretim elemanı | 456 |
| Program | 104 |
| Ders | 9.346 |
| Müfredat bağlantısı | 12.591 |

Toplam 22.641 katalog kaydı vardır. Önceki veri düzeltmeleri yerel UniversityDecisionHub veritabanına uygulanmıştı. Bu paket hazırlanırken yeniden aktarım yapılmadı. Hedef demo bilgisayarı yeni sürüm olarak çalıştırılmış sayılmaz. Kaynakta hazır olmak, belirli bir veritabanına uygulanmış olmakla aynı değildir.

Son dosya denetiminde Elektrik-Elektronik müfredatındaki **20 boş kredi alanı**, ders kodu/adı/kart kimliği ve resmî kredi değeri eşleştirilerek tamamlandı. Bunlar JSON, CSV ve Excel'de günceldir; **veritabanına henüz uygulanmadı**. Güncel dosya ile mevcut yerel veritabanının tek farkı bu 20 CourseCredit alanıdır; salt okunur tam alan sorgusunda doğrulandı. `Veri/Entegrasyonda_Uygulanacak_20_Kredi_Duzeltmesi.csv` ve `Kontroller/20_Kredi_Duzeltmesini_Uygula.sql` bunun içindir. Sonraki entegrasyonda SQL eski değerin boş veya zaten doğru olduğunu kontrol eder; beklenmeyen değerde durur. Katalog aktarıcısı mevcut alanları kendiliğinden güncellemediği için yalnızca import komutunu çalıştırmak bu alanları düzeltmez.

100 bölümde program vardır. Programı olmayan kalan 35 kayıt 26 alternatif/ortak kadro birimi ve 9 akademik birim olarak korunur. Sıfır program sayısını sıfıra indirmek amacıyla yeni, gerçekte olmayan programlar eklenmez. 82 doğrulanmış kadro bölüm düzeltmesi katalogda korunmuştur.

## Gerçekten açık kalanlar

- Kredi, yarıyıl, önerilen sınıf ve tam CourseDescription boşlukları raporlanmıştır. Bilinmeyenler sıfır veya tahminî değerlerle doldurulmadı. 8.401 kartın kısa alıntısı tam içerik değildir.
- 84 program için bu teslimde kaynak plan satırları eşleşir. Diğer tarihsel programları bugün yeniden araştırılmış gibi göstermedik; 104 programın tamamı güncel JSON'da korunur ve müfredat bağlantısı vardır.
- GİT 1002'nin iki farklı kaynak dersi ayrı kararı bekler. Kullanıcı ertelediği için kod/şema değiştirilmedi.
- Çoklu kaynak kadro üyeliğinin yeni demo API'sinde sunulması, kayıt formundaki bölüm seçenekleri ve DepartmentCode/DepartmentKey etiket koşulu ekip kodu incelemesidir.
- Yeni demo dalında AddReviewModeration migration'ı vardır. Hedef veritabanında uygulanma durumu entegrasyonda okunarak kontrol edilmelidir. Bu turda uygulanmadı.
- 23 anket yanıtı anahtarlarla eşleşir. İki mezunda isGraduate=true ve studyYear boş; hesap aktarımı beklemededir. İsim, kişisel e-posta, parola veya gerçek hesap verisi bu pakete eklenmez.

## Eski dosyalar

38 eski Excel / 107 sayfa ve 22 ZIP tarandı. Excel'de kayıtlı hata hücresi veya okunamayan çalışma kitabı, ZIP'lerde bütünlük hatası bulunmadı. Bu sonuç eski sürümlerin güncel veya bütün kaynak bilgilerinin doğru olduğu anlamına gelmez. Eski Excel/ZIP dosyaları değiştirilmedi, silinmedi. Envanter ve parmak izleri pakettedir; arşivlenecek dosyalar bu listeyle izlenebilir.

Eski çalışma dosyalarını topluca GitHub'a koymayın. Güncel veri dosyaları, gerekli kaynak kanıtları ve belgeler seçilmelidir. Veritabanı yedekleri, secrets.json, obj/bin, kişisel anket dosyaları ve onaysız uygulama yamaları paylaşılmamalıdır.

## Kod sahipliği

Güncel kod ayrı klasörde korunur: UniversityDecisionHub_Guncel_2026-11-19, yerel codex/github-demo-2026-11-19 dalı, GitHub feature/democontrol @ 5b79729 temeli. Eski çalışan klasörün yerel değişiklikleri bunun üzerine taşınmadı. Önceki beş kod dosyası değişikliği tarihsel inceleme paketinde bulunur; bu veri teslimi otomatik kod birleştirmesi yapmaz. Ekipte esas dal teyidi ve ilgili kod sahiplerinin incelemesi gerekir.
