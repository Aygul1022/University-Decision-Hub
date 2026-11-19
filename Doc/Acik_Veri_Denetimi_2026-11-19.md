# Açık veri maddelerinin kontrolü — 19 Kasım 2026

Bu kontrol katalog JSON'unu, veritabanını veya uygulama kodunu değiştirmedi. 104 programlı paket ve önceki yerel entegrasyon sonucu korunur. Sunum çalışması yapılmadı.

## Sami'nin son mesajı

Kullanıcının ilettiği Sami mesajı, `36fd4d0` ile yerel kurgu hesap provasının geçtiğini bildiriyor: kayıt/giriş, profil kaydetme, Bilgisayar Mühendisliğinde 175 ders, farklı hesapla yorum okuma ve API yeniden başlatma sonrası kalıcılık. Yönetici gizleme, geri açma ve silme de Sami tarafından geçti olarak bildirildi. Bunlar bu turda bağımsız yeniden çalıştırılmış testler değildir. Ayrı kanıt türüyle kaydedildi.

Sami'nin açık bıraktığı EEM kayıt seçenekleri veri tarafında kontrol edildi:

| Bölüm anahtarı | Fakülte/birim | Program bağlantısı | Veri kararı |
|---|---|---|---|
| DEP-0005 | FAC-002, Lisansüstü Eğitim Enstitüsü | PRG-0020 yüksek lisans ve PRG-0021 doktora | Derece ayrımı korunur. |
| DEP-0065 | FAC-004, Mühendislik ve Doğa Bilimleri Fakültesi | PRG-0019 lisans | Lisans öğrencisinin doğru program bağlantısı. |
| DEP-0112 | FAC-004 | Program yok | Eski anahtar korunur; yeni öğrenci seçiminde programlı seçenek gibi gösterilmemeli. |

İsim benzerliği bu üç kaydı tek satıra indirme gerekçesi değildir. Ekran tarafındaki seçim/etiket davranışı ilgili kod sahiplerinin işidir; bu turda değiştirilmedi. Sayısal yerel ID'ler koda sabitlenmemelidir.

## Tarihsel 20 programın kaynak kontrolü

Önceki derlemede 84 programın doğrudan müfredat kaynak eşleştirmesi vardı. Kalan 20 program için resmî kaynak sayfaları yeniden incelendi:

- 20 programın resmî sayfası indirildi; adres, erişim tarihi ve sayfa özeti saklandı.
- 19 programda toplam 36 ders planı bağlantısı bulundu ve takip edildi.
- Bu 36 bağlantının 34'ünde gerçek ders satırları okundu: toplam 1.613 kaynak satırı.
- Mevcut 20 programın 1.805 tarihsel müfredat satırından 1.439'u, bulunan planlarda ders kodu/adı düzeyinde eşleşti. Kalan 366 satır için aynı düzeyde eşleşme çıkmadı.

Kod/ad eşleşmesi müfredat yılı, tezli/tezsiz varyantı, kredi veya bütün alanların doğrulandığı anlamına gelmez. Eşleşmeyen satırlar da doğrudan hatalı ilan edilmez; eski katalog yılı, farklı ad veya farklı program varyantı olabilir. Kayıt silinmedi ve program bağlantısı tahminen taşınmadı.

Somut açıklar:

- `PRG-0058` Küresel İlişkiler ve Avrupa Birliği sayfasında program tanıtımı var; ders planı bağlantısı bulunamadı. Eski 56 satır korunur, güncel plan teyidi bekler.
- Finansal İktisat doktora İngilizce planı (Bologna 4090) ile Lojistik bütünleşik doktora planı (4102) bu kontrolde gerçek ders satırı döndürmedi. Bu durum programların kaldırıldığını kanıtlamaz.
- EEM doktora, İletişim Bilimleri yüksek lisans ve Özel Hukuk yüksek lisans için toplam 310 tarihsel satırın kod/ad eşleşmesi çıkmadı; seçili yıl ve program varyantı ayrı incelenmelidir.
- Birden fazla tezli/tezsiz veya bütünleşik planı olan genel yüksek lisans/doktora kaydına tek Bologna kimliği otomatik atanmadı.

Kaynaklar: [yüksek lisans programları](https://lisansustu.dogus.edu.tr/programlar/yuksek-lisans-programlari), [doktora programları](https://lisansustu.dogus.edu.tr/programlar/doktora-programlari), [Hemşirelik](https://sbyo.dogus.edu.tr/bolumler/hemsirelik), [Küresel İlişkiler](https://lisansustu.dogus.edu.tr/programlar/yuksek-lisans-programlari/kuresel-iliskiler-ve-avrupa-birligi). Programa ve plana özgü adresler kanıt CSV'lerindedir. Ham sayfalar özel bilgi içermeyen yerel kaynak önbelleğinde korunur; GitHub'a tam HTML dökümü eklenmez.

## Boş alanlar

Güncel JSON'da 5.700 müfredat kredi alanı, 130 yarıyıl ve 12.591 önerilen sınıf alanı boştur. Bu sayıların tamamı hata değildir. Dönemden sınıf veya ders saati toplamından kredi türetilmedi.

Mevcut doğrudan eşleştirilmiş kaynak kanıtları üzerinde kontrol, tek ve tartışmasız değerle doldurulabilecek yeni boş kredi/yarıyıl adayı vermedi. Bu, üniversitenin başka hiçbir kaynağında bilgi olmadığı anlamına gelmez. Yeni bulunan tarihsel planlar, yıl/varyant eşleştirmesi tamamlanmadan mevcut boş alanlara kopyalanamaz.

## Kapanış durumu

- Yerel aktarım, tam alan karşılaştırması ve kullanıcı akışı: önceki kanıtlarla tamamlandı; Sami'nin ek yerel prova bildirimi kaydedildi.
- Ortak demo: Aygül'ün adres/bilgisayar, kesin commit ve yüklü katalog teyidi bekleniyor. Güncel sürüm ve katalog zaten varsa yeniden birleştirme veya tekrar aktarım yapılmamalı.
- Anket: hesap sahipliği/parola tamamlama ve aktarım sözleşmesi bekleniyor; gerçek hesap aktarılmadı.
- EEM seçenekleri ve diğer arayüz/kadro gösterimi: ilgili kod sahiplerinin incelemesi bekleniyor.
- Tarihsel kaynak eşleştirmesi ve bulunamayan alanlar: yukarıdaki kayıtlarla açık takipte. Güncelmiş gibi işaretlenmedi.
- GİT 1002: kullanıcının erteleme kararı korunur.

Kanıtlar: `Doc/Verification/open-data-2026-11-19/`. Önceki Excel'deki açık konu sayfası tarihsel teslim durumunu gösterir; güncel durum için bu not ve `Acik_Konular_ve_Sorumlular.csv` esas alınmalıdır.
