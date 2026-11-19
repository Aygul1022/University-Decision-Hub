# Anket aktarımı: veri hazırlığı ve teknik engeller

23 anonim yanıtın bölüm/program eşleştirmesi güncel pakette bulunur; iki mezunda sınıf boş tutulur. Bu liste hesap oluşturma dosyası değildir; isim, e-posta veya parola içermez. Hedef bilgisayarda yerel kimlikler kaynak anahtarlarından yeniden çözülmelidir.

## İncelenen sürümün mevcut davranışı

Temel: `feature/democontrol @ 5b79729`; veri dalında uygulama kodu değiştirilmedi.

- `UserImportCommand`, yalnızca `FullName,Email,DepartmentId,StudyYear` CSV başlığını kabul eder. `ProgrammeId` ve `IsGraduate` desteği varsayılmamalıdır.
- `AuthController.Register` parolasız ön kayıt satırını 403 ile reddeder. E-postayı bilerek hesabı sahiplenme yolu kapalıdır.
- Yorum okuma oturum gerektirir. Program/sınıf uygunluğu yazmada aranır; programı okumada da zorunlu yapmak yeni ürün kararıdır.
- Mezunda `IsGraduate=true`, `StudyYear=null` olmalıdır. Tek başına boş sınıf mezunluk kanıtı değildir.

## Ekipten gereken kararlar

1. Aktarımı bekletme kararı değişti mi; gerçek anket hesapları artık aktarılacak mı?
2. Mevcut UserAccounts modeli mi kullanılacak; ayrı tablo gerekiyorsa gerekçesi nedir?
3. Ön kayıt sahibinin e-posta/davet doğrulaması nasıl yapılacak?
4. Kullanıcı parolasını hangi doğrulanmış hesap tamamlama ekranında belirleyecek?
5. Program koşulu yorum okumada da mı uygulanacak; mezunların hakları ne olacak?
6. Program/mezun alanlarını kabul eden kesin aktarım sözleşmesi nedir?

Ümran'ın sonraki işi onaylanan sözleşmeye göre özel dosyayı hazırlamak, hedef anahtarları çözmek, önce kurgu hesapla denemek ve gerçek aktarımı kontrol etmektir. Doğrulama, EF/migration ve ekran kodları bu veri hazırlığında değiştirilmedi. Parolalar Excel/CSV'ye alınmamalıdır.

Kararlar ve teknik destek olmadan gerçek hesap aktarımı tamamlanmış sayılamaz.
