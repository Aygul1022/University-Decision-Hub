# Doğuş Üniversitesi MYO ders ve öğretim elemanı kataloğu

[MYO Programlar sayfasındaki](https://myo.dogus.edu.tr/programlar) 47 programın Ders Planı ve Yapısı bağlantısı izlendi. Bologna'da Dersler sekmesi ve açılışta seçili müfredat esas alındı. Yıl menüsünde yayımlanan diğer plan seçenekleri program dosyasında bulunur.

| Alan | Kayıt |
|---|---:|
| Program | 47 |
| Ders–yarıyıl satırı | 3595 |
| Benzersiz Bologna ders kartı | 3226 |
| Bologna kartında Dersi Verenler alanı dolu | 1485 |
| Dersin İçeriği alanı dolu | 1617 |
| Ayrı tutulan seçmeli grup başlığı | 299 |
| Kartın ayrıntıları erişilebilir | 1619 |
| Kart boş veya erişilemedi | 1607 |

- `01_Programlar.csv` programın MYO ve Bologna adını, müfredat yılını ve kaynak bağlantısını verir.
- `02_Dersler_ve_Yariyillar.csv` her gerçek dersi, yarıyılını ve ders kartını; yayımlanıyorsa yerel krediyi ve öğretim elemanı adını gösterir. v7 anahtarı, birebir program planı ve ders eşleşmesi bulunduğunda yer alır.
- `03_Gercek_Ders_Kartlari.csv` öğretim elemanlarını, koordinatörü ve ders içeriğinin kısa önizlemesini içerir. Tam içerik her satırda bağlantısı bulunan resmî kartta okunabilir.
- `04_Secmeli_Grup_Basliklari.csv` grubun adı ve seçilecek ders sayısını verir. Grubun içindeki gerçek dersler ikinci dosyada yer alır.
- `05_Hoca_Adi_Yayinlanmayan_Kartlar.csv` Dersi Verenler alanı boş olan kartları gösterir.

Öğretim elemanı bilgisi kartta yazan katalog bilgisidir. Gerçek akademik yıl, dönem ve şube bazındaki ders açılışını teyit etmek için okulun ders programı veya resmî atama verisi gerekir. Mahkeme Büro Hizmetleri için MYO’nun yönlendirdiği plan ile Bologna’daki plan başlığı ayrı ayrı korundu. Paket kaynak verisidir ve hedef PostgreSQL’e aktarılmadı.

**v7 karşılaştırması.** MYO menüsündeki İş Sağlığı ve Güvenliği ve Sosyal Hizmetler için mevcut v7 program anahtarı bulunamadı. Her iki programın dersleri pakete dahil. `06_V7_Katalog_Eslesmesi_Olmayanlar.csv` v7 müfredat anahtarı bulunamayan 175 satırı gösterir. İkinci dosyanın seçmeli grup alanları Bologna'daki açılır grup üyeliğini gösterir.
