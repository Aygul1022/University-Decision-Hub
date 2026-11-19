# 19 Kas?m 2026 - g?ncel katalog entegrasyonu

Bu dal yaln?zca veri, kaynak kan?tlar? ve belgelendirme i?erir. Uygulama kodu de?i?tirilmedi. Temel: feature/democontrol, 5b79729. Ortak dala merge yap?lmad?.

## Kullan?lacak dosyalar

- DataBase/Imports/catalog.ready.json: esas, schemaVersion 2 katalog.
- Esas aktar?m yolu mevcut import-catalog arac?d?r. Eski catalog.ready.insert-only.sql giri?ine a??klay?c? durdurma eklendi; 92 programl? eski SQL history/ alt?nda korunur ve g?ncel paket olarak kullan?lmaz. Yeni tam SQL denemesi mevcut algoritman?n 120 saniye s?n?r?na tak?l?p geri al?nd??? i?in desteklenen aktar?m yolu olarak sunulmad?. Uygulama aktar?m arac? ba?ar?yla do?ruland?.
- DataBase/Sources/catalogue-2026-11-19/Katalog_Guncel_2026-11-19.xlsx: 19 sayfal?k ?al??ma kitab? ve kaynak kan?tlar?.
- DataBase/Imports/2026-11-19/: dar kapsaml? d?zeltmeler ve kontrol sorgular?. Dosyalar?n tamam?n? s?rayla rastgele ?al??t?rmay?n. Yerel say?sal ID'ler ta??nabilir kimlik de?ildir.

## Bu bilgisayarda do?rulanan

Yerel UniversityDecisionHub / port 5432. Yedek al?nd? ve ar?iv i?eri?i okunabildi; yedek ve ba?lant? bilgileri GitHub'a eklenmedi. Kaynakla do?rulanm?? 20 CourseCredit alan? null de?erinden do?ru de?ere g?ncellendi. Katalog tam alan kar??la?t?rmas? s?f?r fark; dokuz tekrar kontrol?nde s?f?r grup. Kredi g?ncellemesi boyunca hesap ve yorumlar?n b?t?n sat?r parmak izleri korundu.

9 fak?lte, 135 b?l?m, 456 ??retim eleman?, 104 program, 9346 ders, 12591 m?fredat ba?lant?s?. Toplam 22641. Ger?ek uygulama aktar?m arac? tekrar ?al??t?r?ld?: 0 added, 22641 unchanged; yeni m?kerrer kay?t, g?ncelleme veya silme yok.

Mevcut 20261116191117_AddReviewModeration migration'? bu yerel veritaban?na uyguland?. Yeni migration ?retilmedi. ?? Reviews s?tunu kontrol edildi. Eski ?al??an API kapat?lmad?; g?ncel API ayr? 5156 portunda salt okunur katalog kontrol? i?in ba?lat?ld?. 104 program, 135 b?l?m ve 456 ??retim eleman? HTTP ile okundu; Bilgisayar M?hendisli?i b?l?m/program filtresi do?ruland?. Yeni s?r?mde kay?t/yorum g?nderme/y?netici silme u?tan uca testi bu turda yap?lmad?; ger?ek yorum silinmedi.

## Di?er bilgisayarda yap?lacak

Git pull veritaban?n? g?ncellemez. Hedef/port/yedek/migration kontrol?nden sonra tam alan kar??la?t?rmas?n? ?al??t?r?n. Bo? hedefte g?ncel JSON aktar?labilir. Eski 92 programl? hedefte mevcut alan ve kadro ili?kisi farklar? incelenmeli; anahtar de?i?tirme, sil-yeniden ekle veya zorla g?ncelleme yapmay?n. Ge?mi? 62 alan d?zeltmesinin tamam? bu paketteki 20 alan SQL'inin i?inde de?ildir. Teknik veri sahibiyle hedefe ?zel fark plan? haz?rlay?n.

Paketin ?nceki README_TESLIM_ONCESI.md dosyas? tarihsel durumu anlat?r; 20 kredi art?k bu bilgisayarda uygulanm??t?r. Ekip demo/hedef PostgreSQL'i ayr?ca do?rulanmad?. Anket kullan?c?lar? eklenmedi. Kaynakta do?rulanmayan de?erler null tutulur. G?T 1002 konusu ertelenmi?tir. 84 program?n plan kan?tlar? g?ncel derlemede e?le?ir; kalan tarihsel programlar yeniden ara?t?r?lm?? gibi sunulmaz. ?oklu kadro API deste?i ve profil/kay?t etiketleri i?in onays?z eski kod yamas? ta??nmad?.

JWT anahtar? bu yerelde tan?ml? de?il; API yeniden ba?lay?nca giri? oturumlar? d??er. Sabit anahtar yaln?zca ?zel yerel ayarda tutulmal?, repoya konmamal?d?r.

SQL performans denemesi s?ras?nda hata/timeout i?lemi geri ald?; kaynak verisi silinmedi. C# API veya aray?z kodu de?i?tirilmeden, katalog aktar?m? uygulaman?n mevcut arac?yla tamamland?.
