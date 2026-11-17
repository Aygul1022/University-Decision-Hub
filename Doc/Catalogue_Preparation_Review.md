# Katalog hazırlık paketi: aktarım öncesi doğrulama ve düzeltme planı

17 Kasım 2026. Kaynak: `UniversityDecisionHub_Catalogue_Preparation_2026-11-15.zip` (Ümran'ın hazırlık paketi). Paketin dosyaları `DataBase/Sources/preparation-2026-11-15/` altına konuldu. Pakette öğrenci listesi, e-posta ya da parola yoktur (hücreler e-posta ve uzun rakam dizileri için taranıp bulunmadı).

**Bu belge yalnızca plandır. Hiçbir veritabanına bir şey uygulanmadı.** Karşılaştırmalar, repodaki `DataBase/Imports/catalog.ready.json` dosyasıyla yapıldı; bu dosya, Ozan'ın yerel veritabanına aktarılan katalogdur. Aygül'ün demo veritabanına bakılamadı; orada çalıştırılacak salt okunur sorgular en altta.

## 1. Paket ile repodaki katalog arasındaki fark

| Veri | Paket | Repo (`catalog.ready.json`) | Sonuç |
|---|---:|---:|---|
| Fakülte | 9 | 9 | Aynı anahtarlar ve adlar |
| Bölüm | 129 | 130 | Repoda fazla olan tek kayıt: `DEP-0086` Psikoloji. Diğer alanlarda fark yok |
| Program | 92 | 92 | Aynı anahtarlar; bölüm, ad, derece, dil ve Bologna kimliği alanlarında fark yok |
| Öğretim elemanı | 449 | 452 | Repoda fazla olanlar: 396, 397, 398 (hepsi `DEP-0086`). Bölüm ve ad farkı yok |
| Ders / müfredat | dosya yok | 303 / 316 | Pakette ders aktarım dosyası yok; 5.442 ham satır hazırlama arşivinde |

Yani paket, repodaki kataloğun doğrulanmış çekirdeğidir. Tek ayrışma, paketin "bekliyor" dediği `DEP-0086` ve ona bağlı üç öğretim elemanıdır.

## 2. Paketteki kararların repodaki durumu

| Karar | Paket | Repoda şu an | Yapılacak |
|---|---|---|---|
| Eski bölüm anahtarlarını kaldır: `DEP-0006`→`DEP-0005`, `DEP-0008`→`DEP-0007`, `DEP-0018`→`DEP-0017` | Onaylı | Eski üç anahtar katalogda yok; programlar zaten kanonik bölümlerde | Uygulanacak bir şey yok. Demo veritabanında doğrulanmalı (§5, sorgu 1) |
| `DEP-0016` ve `DEP-0087` fakültesi `FAC-008`→`FAC-002` | Onaylı | İkisi de `FAC-002` | Uygulanacak bir şey yok. İçe aktarma güncelleme yapmadığı için demo veritabanı eski JSON'dan yüklendiyse hâlâ `FAC-008` olabilir (§5, sorgu 2) |
| `DEP-0086` Psikoloji | Bekliyor: fakülte anahtarı onaylı değil, aktarma | Var, fakülte `FAC-008`, üç öğretim elemanı bağlı | **Dokunma.** Aygül'ün isteği gereği mevcut öğretim elemanı bağlantıları korunur. Fakülte, Ümran'ın onayını bekliyor |
| `DEP-0092` İletişim Bilimleri (lisans) | Bekliyor | Yok | Aktarma |
| Bekleyen üç program (İletişim Bilimleri lisans, Mütercim Tercümanlık, Psikoloji lisans) | Bekliyor, aktarma | Yok | Aktarma |

Sonuç: onaylanmış kararların hiçbiri için ek bir veritabanı değişikliği gerekmiyor. Gerekli olan, bu durumun demo veritabanında doğrulanması.

## 3. Aynı adlı bölümler neden var

Programı olan 89 bölümün 3'ünde aynı ad iki kez geçiyor. Üçünde de lisans programı kendi fakültesindeki bölümde, yüksek lisans ve doktora Lisansüstü Eğitim Enstitüsü (`FAC-002`) altındaki bölümde:

| Ad | Lisans bölümü (fakülte) | Yüksek lisans / doktora bölümü |
|---|---|---|
| İşletme | `DEP-0094` (`FAC-008`, İktisadi, İdari ve Sosyal Bilimler) | `DEP-0017` |
| Elektrik-Elektronik Mühendisliği | `DEP-0065` (`FAC-004`, Mühendislik ve Doğa Bilimleri) | `DEP-0005` |
| Business Administration | `DEP-0081` (`FAC-008`) | `DEP-0003` |

Bu hata değil. Paketin "aynı fakülte, aynı ad" kuralı yalnızca aynı fakültedekileri birleştirir; bunlar farklı fakültelerde. Bu yüzden **bölümler birleştirilmemeli**. Listede ayırt edilmeleri bir ön yüz konusudur (etikete derece ya da fakülte eklenmesi) ve bu planın dışındadır.

## 4. Ders ve müfredat kapsamı

### Mevcut durum
Müfredatı olan yalnızca üç program var: Anestezi `PRG-0002` (5 kayıt), Elektrik-Elektronik Mühendisliği lisans `PRG-0019` (214), İşletme lisans `PRG-0050` (97).

### Hazırlama arşivi (`course_staging_v2.xlsx`, 5.442 satır)
- 58 kaynak program grubu: 56'sı "tek aday" (adı birebir tek programla eşleşiyor), 2'si bekliyor (Elektrik-Elektronik Mühendisliği 220 satır, İşletme 97 satır; birden çok aday program).
- Satırı olan program: **56** (33 ön lisans, 21 lisans, 2 yüksek lisans). Hiç satırı olmayan program: **36** (12 ön lisans, 11 yüksek lisans, 8 lisans, 5 doktora). Satırı olmayan lisans programları: Elektrik-Elektronik Mühendisliği, Hemşirelik, Hukuk, Economics, English Language and Literature, İngilizce Mütercim ve Tercümanlık, İşletme, Business Administration.
- Computer Engineering `PRG-0006`: 240 satır, 183 farklı ders kodu, 33 zorunlu + 207 seçmeli satır, tamamında AKTS dolu, dönemler 1–8. Dersi olmayan öğrencinin görmeyi beklediği program budur; müfredat verisi arşivde var ama aktarılmamış.
- Anestezi: arşivde 41 satır (39 farklı kod), katalogda yalnızca 5. Kalan 36 satırın dönem, yıl ve zorunluluk bilgisi "Not publicly published".
- Dönem ve yıl bilgisi olmayan satır: 1.574. Zorunluluk bilgisi dört farklı biçimde (Zorunlu, Seçmeli, Compulsory, Elective) ve bir "yayımlanmamış" değeriyle geliyor; `IsElective` için tek bir eşleme kuralı gerekir.

### Aktarımı engelleyen ya da karar bekleyen noktalar
1. **Sorumlu bölüm yok.** `responsible_department_key` 5.442 satırın hiçbirinde dolu; durum "Pending — responsible Department not supplied by source". `Course.DepartmentId` zorunlu. Pilot, bunu programın bölümünden türetmişti ("katalog bağlamı"); bunun tüm programlar için kabul edilip edilmeyeceğine karar verilmeli. Paket bilerek bölüm tahmin etmiyor.
2. **Elektrik-Elektronik ve İşletme kimliği.** Paket, bu iki grubun 317 satırını "program kimliği onaylanmadı" diye bekletiyor: EEM için kaynaktaki Bologna kimliği 4193, resmî lisans sayfası 4128 ve eşitlikleri kanıtlı değil; İşletme satırlarında resmî kimlik hiç yok. **Veritabanında ise 214 EEM ve 97 İşletme satırı zaten Lisans programlarına bağlı.** Demoda yorum yazılabilen iki program bu eşlemeye dayanıyor. Ümran'ın onayı gerekiyor; onay gelmezse bu bağlantılar geri alınmalı mı, korunmalı mı kararlaştırılmalı.
3. **Yıl kuralı.** Bir dersin müfredatta birden çok çalışma yılında geçmesi durumunda yorum formu o dersi reddediyor ("Conflicting curriculum study years"). Şu an EEM müfredatı yıl bilgisi boş aktarıldığı için bu kural tetiklenmiyor. Yıl, dönemden türetilirse (dönem 5 ve 7 → 3. ve 4. yıl): Computer Engineering'de dönemi olan 183 kodun 54'ü (seçmeli havuz, 5. ve 7. dönemde tekrar) bu duruma girer; 56 programın 15'inde en az bir ders girer. Yıl boş bırakılırsa "önerilen yıldan küçük olamaz" kuralı da hiç işlemez. Hangisinin seçileceği ekibin kararı.
4. **Anahtar çakışması.** Dersler `(bölüm, ders kodu)` ile benzersiz. Sorumlu bölüm programın bölümünden türetilirse, aynı bölümdeki iki programda aynı kodlu farklı adlı ders çıkabilir. `validate-catalog` bunu aktarımdan önce yakalar; arşiv satırlarından üretilecek dosya bu denetimden geçirilmeden uygulanmamalı.
5. **Resmî Bologna kimlikleri.** Programların `BolognaProgrammeId` alanı katalogda boş. `official_programme_evidence_v2.xlsx` altı program için doğrulanmış kimlik içeriyor (EEM lisans 4128, EEM yüksek lisans 4136, İşletme lisans 3, İşletme yüksek lisans 4094;4098, İşletme doktora 4099; EEM doktora bekliyor). Aktarım var olan kaydı güncellemediği için bu alan ayrı, gözden geçirilmiş bir düzeltme gerektirir.

## 5. Önerilen sıra (hiçbiri uygulanmadı)

1. **Salt okunur doğrulama, demo veritabanında** (aşağıdaki sorgular). Çıktılar Ümran'a ve bana iletilir.
2. **Kararlar** (Ümran ve ekip): bekleyen `DEP-0086` fakültesi; EEM ve İşletme program kimliği; sorumlu bölümün programdan türetilmesi; yıl kuralı; `IsElective` eşlemesi.
3. **Ders aktarım dosyası** yalnızca "tek aday" grupların satırlarından, ayrı bir dosya olarak (`catalog.courses.next.json` gibi) üretilir; mevcut `catalog.ready.json` değişmez. Önce Computer Engineering ve Anestezi'nin kalan satırları.
4. **`validate-catalog`** ile doğrulama (veritabanına bağlanmaz), ardından **geçici bir veritabanı kopyasında** `import-catalog` denemesi ve sayı karşılaştırması. Aktarıcı yalnızca ekler, hiçbir kaydı güncellemez ya da silmez; beklenen sonuç "N added, 0 unchanged, 0 updated".
5. Etki incelemesi sonrası demo veritabanına uygulama.

Bu plan öğretim elemanı, bölüm, kullanıcı ya da mevcut yorum kayıtlarına dokunmuyor. Yeni ders ve müfredat satırları yalnızca eklenir; var olan kullanıcıların `DepartmentId` ve `ProgrammeId` değerleri değişmez.

## 6. Demo veritabanında çalıştırılacak salt okunur sorgular

Kimlik numaraları veritabanına göre değiştiği için anahtarla çalışılır.

```sql
-- 1. Emekli edilen bölüm anahtarları katalogda olmamalı (beklenen: 0 satır)
select "DepartmentKey", "DepartmentName" from "Departments"
where "DepartmentKey" in ('DEP-0006', 'DEP-0008', 'DEP-0018');

-- 2. Onaylanan fakülte düzeltmeleri (beklenen: ikisi de FAC-002)
select d."DepartmentKey", d."DepartmentName", f."ImportKey" as faculty
from "Departments" d join "Faculties" f using ("FacultyId")
where d."DepartmentKey" in ('DEP-0016', 'DEP-0087');

-- 3. Bekleyen kayıtlar ve bağlı kullanıcı/öğretim elemanı sayıları
select d."DepartmentKey", d."DepartmentName", f."ImportKey" as faculty,
       (select count(*) from "Instructors" i where i."DepartmentId" = d."DepartmentId") as instructors,
       (select count(*) from "UserAccounts" u where u."DepartmentId" = d."DepartmentId") as users
from "Departments" d join "Faculties" f using ("FacultyId")
where d."DepartmentKey" in ('DEP-0086', 'DEP-0092');

-- 4. Müfredatı olan programlar (beklenen: PRG-0002, PRG-0019, PRG-0050)
select p."ProgrammeKey", p."ProgrammeName", p."DegreeLevel", count(*) as curriculum_rows
from "Programmes" p join "ProgrammeCourses" pc using ("ProgrammeId")
group by 1, 2, 3 order by 1;

-- 5. Programı olmayan bölüm sayısı (beklenen: 41, toplam 130 bölümde)
select count(*) from "Departments" d
where not exists (select 1 from "Programmes" p where p."DepartmentId" = d."DepartmentId");

-- 6. Kullanıcıların seçtiği programlar: etkilenebilecek hesaplar
select p."ProgrammeKey", p."ProgrammeName", count(*) as users
from "UserAccounts" u join "Programmes" p on p."ProgrammeId" = u."ProgrammeId"
group by 1, 2 order by users desc;
```

## 7. Bu belgenin kapsamı dışında kalanlar

Aynı adlı bölümlerin listede ayırt edilmesi, kayıt ekranındaki bölüm listesi, mezunların yorum yazma kararı ve v2 (96 programlı) paketin ayrıca doğrulanması. Bu belge o pakete değil, 15 Kasım hazırlık paketine bakar.
