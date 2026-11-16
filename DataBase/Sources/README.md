# Katalog kaynakları ve dönüşüm

Hazırlama tarihi: 16 Kasım 2026. Kaynak paket Aygül'ün `C:/Users/aygul/OneDrive/Desktop/Datas2` klasörüdür. Başka bilgisayarlarda bu yolun bulunması gerekmez; aktarım yalnız Imports/catalog.ready.json kullanır. Kaynak Excel'ler değiştirilmedi veya bu klasöre kopyalanmadı. Ümran'ın kaynak paketi gerektiğinde ayrıca ekip içinde paylaşılır.

`catalog.sources.json`, kaynak dosya adları/SHA256 özetlerini ve üretilen JSON'un SHA256 özetini kaydeder. Bunlar kaynak sürümünü karşılaştırmak içindir; resmî doğruluğun bağımsız kanıtı değildir.

- Faculty.xlsx, Departments.xlsx, Programmes.xlsx ve Instructors.xlsx: yalnız import sözleşmesindeki alanlar alınır; sabit anahtarlar metin olarak korunur. Kaynak URL/notları Excel'de kalır.
- Course_Pilot_Anestezi.xlsx / Course: course_key, department_key, course_code, course_name ders kimliğine aktarılır.
- Aynı dosyanın ProgrammeCourse sayfası course_key üzerinden Course sayfasıyla eşleştirilir. Kredi, AKTS ve is_elective buradan müfredat kaydına alınır; yıl/dönem/önerilen sınıf bilinmiyorsa null korunur. ANS107 false; diğer dört dersin seçmeliliği null.
- Pilot department_key doğrulanmış programdan türetilmiş katalog bağlamıdır; bağımsız sorumlu/dersi veren bölüm doğrulaması değildir.
- Course_Staging.xlsx içindeki 5442 satır bu JSON'a aktarılmadı. EEM için bildirilen 2025 kaynağı 2026 diye yeniden etiketlenmedi; staging sonraki doğrulanmış ders grupları için korunur.
- Ders açılışı veya öğretim elemanı ataması üretilmedi; T+U+L için yeni DB alanı eklenmedi.

Önceki private-data/catalog.pilot.v2.json ile altı koleksiyon karşılaştırıldı: eski satır değişikliği/eksilmesi yok; DEP-0086 ve instructorKey 396, 397, 398 eklendi. JSON alanlarının tipi, kaynak anahtarlarının benzersizliği ve ANS107 false değeri dönüşümde kontrol edildi. Uygulamanın validate-catalog komutu ayrıca çalıştırılır; bu kontrol gerçek DB'ye uygulanmış olduğunu göstermez.
