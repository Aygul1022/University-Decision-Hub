# Güncel veri sözlüğü ve normalizasyon notları

19 Kasım 2026. Şema dayanağı: `feature/democontrol`, `5b79729`, EF Core model görüntüsü. Eski gereksinim taslağı ve PDF çizimi planlama geçmişidir; mevcut uygulamanın bütün alanlarını içermez. Ayrıntılı 11 tablo/alan listesi `Veri_Sozlugu_Alanlari.csv` ve ana Excel'in `Veri_Sozlugu` sayfasındadır. Bu belge şema değişikliği veya migration üretmez.

## Katalog kimlikleri

Katalog JSON'u `schemaVersion=2` sözleşmesini kullanır. facultyKey, departmentKey, instructorKey, programmeKey, courseKey ve curriculumKey sabit kaynak kimlikleridir. Veritabanının ürettiği sayısal kimlikler farklı bilgisayarlarda değişebilir; dosyalarda yerel DepartmentId/ProgrammeId değerleri sabit kullanılmaz.

Faculties.ImportKey ve Instructors.ImportKey, sırasıyla JSON facultyKey/instructorKey değerlerini taşır. Departments.DepartmentKey bölümün kaynak anahtarıdır. DepartmentCode farklı, isteğe bağlı resmî kod alanıdır; benzersiz kaynak anahtarı değildir. Yeni profil kodundaki `DepartmentCode == DEP-0064` koşulu ayrı ekip inceleme konusudur.

## Tablo görevleri

| Tablo | Görev ve ana kurallar |
|---|---|
| Faculties | Fakülte/enstitü/yüksekokul/birim kökü; FacultyId PK, ImportKey benzersiz kaynak kimliği |
| Departments | Fakülteye bağlı bölüm veya akademik birim; DepartmentKey benzersiz; her birime diploma programı eklenmesi gerekmez |
| Instructors | Giriş hesabından ayrı öğretim elemanı; birincil DepartmentId FK; aynı isim farklı kişi olabilir |
| Programmes | Bir bölüme bağlı derece/dil/program kimliği; ProgrammeKey benzersiz; Bologna kimliği kaynakta biliniyorsa kanıt listesinde korunur |
| Courses | Bölüm içindeki ders kodu/adı ve isteğe bağlı açıklama; CourseKey benzersiz, DepartmentId+CourseCode benzersiz |
| ProgrammeCourses | Bir dersin program müfredatındaki yıl/yarıyıl/kredi/AKTS/seçmeli bağlantısı; CurriculumKey benzersiz |
| CoursePrerequisites | CourseId+PrerequisiteCourseId bileşik anahtarıyla gerçek önkoşul ilişkisi; yalnızca metinsel kart ifadesinden tahminen FK üretilmez |
| CourseOfferings | Dersin belirli akademik yıl/dönem açılışı; gerçek açılış verisi yoksa uydurulmaz |
| OfferingInstructors | OfferingId+InstructorId bileşik anahtarıyla dönemlik görevlendirme; programın akademik kadro sayfası bunun kanıtı değildir |
| UserAccounts | Tek hesap/profil modeli; normalleştirilmiş e-posta benzersiz, parolanın hash'i saklanır; gerçek hesap verileri bu pakette yoktur |
| Reviews | Kullanıcı, ders, öğretim elemanı, yıl/dönem, devam beyanı ve metin; UserId+CourseId benzersiz; OfferingId isteğe bağlı |

## Müfredat ile ders bilgisinin ayrılması

Ders bir kez tanımlanır. Program, müfredat yılı ve yarıyıl bağlantıları ayrı ProgrammeCourses satırlarında saklanır. Kredi/AKTS veya seçmelilik program/müfredat bağlamına bağlı olabileceği için bu değerleri dersin bütün kullanımları için tek bir evrensel değere dönüştürmek doğru değildir. Eski Courses modelindeki bazı nullable alanların varlığı, JSON CourseRow sözleşmesinde bu alanların aktarılabildiği anlamına gelmez. Güncel aktarım sözleşmesi kredi/AKTS değerlerini müfredat satırında taşır.

Programı olan 100 bölüm, 104 programa bağlıdır. Kalan 35 kayıt 26 eski alternatif/ortak kadro birimi ve 9 akademik birimdir. Aynı ad, farklı derece ve fakültede farklı gerçek programları temsil edebilir. Alternatif adlar ve 82 doğrulanmış birincil kadro düzeltmesi ayrı karar/kanıt tablolarında izlenir. Eski bölüm kayıtlarını silmek veya kullanıcı seçimlerini otomatik taşımak bu teslimin parçası değildir.

## Kadro üyeliği ile dönemlik ders verme ilişkisi

Bir kişinin birden fazla program sayfasında yer alması yeni bir kişi oluşturmaz. Birincil bölüm alanı ile çoklu kaynak üyelikleri farklı bilgi türleridir. Paketteki 485 kaynak üyeliği verisi 88 bölüm/program grubu için kayıt listesi olarak korunur; yeni demo API'sine uygulandığı iddia edilmez. İlgili kod desteği ekip incelemesindedir. Gerçek dönem/akademik yıl kanıtı olmadan OfferingInstructors kaydı üretilmez.

## Mezun ve hesap kuralları

IsGraduate=true ise StudyYear null olmalıdır; veritabanı kontrol kısıtı ikisinin birlikte dolu olmasını reddeder. Null sınıf tek başına mezun anlamına gelmez. Anonim anket eşleştirmesinde iki mezun ayrı mezun bayrağıyla gösterilir. 23 anket yanıtının hesap aktarımı beklemededir; bu dosya bir kullanıcı içe aktarma dosyası değildir.

Güncel kayıt kuralı açık kayıttır. Önceden eklenmiş parolasız satırın sahiplenilmesi doğrulama kararı gelene kadar kapalıdır. UserAccount.cs içindeki bazı tarihsel yorumlar bu davranışın yerine sözleşme kabul edilmemelidir; endpoint ve ekipçe kabul edilen davranış belirleyicidir. Mezunların yorum yazma hakkı ayrı ekip kararıdır.

## Yeni yorum moderasyonu alanları

Reviews.IsHidden, ModeratedAt ve ModeratedByUserId, AddReviewModeration migration'ında bulunur. Şemada bulunmaları, hedef PostgreSQL'e uygulandıklarını kanıtlamaz. ModeratedByUserId modelde nullable iz alanıdır; bu belgede tanımlanmamış bir FK ilişkisi varmış gibi gösterilmez. Yönetici silme/gizleme endpoint davranışı ekip kodudur ve bu veri tesliminde değiştirilmedi.

OfferingId dolu olduğunda mevcut şema bileşik ilişkilerle ders açılışı/ders ve görevlendirme/öğretim elemanı tutarlılığını korur. OfferingId boş olduğunda Review yine kendi ders, öğretim elemanı ve dönem alanlarını taşır. Kaynakta dönemlik ders açılışı olmaması mevcut yorum akışında otomatik veri uydurma gerekçesi değildir.

## Bilinmeyen değerlerin anlamı

5.720 müfredat satırında yerel kredi, 130 satırda yarıyıl ve bütün satırlarda RecommendedStudyYear boştur. AKTS alanı boş değildir. Bu değerleri otomatik sıfıra veya yarıyıldan hesaplanan sınıfa çevirmedik. Bu sayılar boşluk tespitidir; bütün boşlukların üniversite tarafından yayımlanmadığını kanıtlamaz. Kaynak eşleştirmesi ve tarihsel verinin kapsamı Excel'de görünürdür.

JSON'daki CourseDescription alanı tüm derslerde boştur. Bu eksikliği gizlemedik: 8.401 kartın yayımlanan içerik bilgisi, kısa alıntıları, önkoşul metinleri ve kaynak URL'leri ayrı sunulur. Kısa alıntı tam açıklama olarak içeri aktarılmadı. Tam içerik gerektiren arayüz davranışı ayrıca değerlendirilmelidir.

## Sınırlar

GİT 1002 için iki farklı ders adı aynı bölüm/kod kısıtına takılmaktadır. İki kaynak satırı korunur, aktarım JSON'u dışında bekler; ders kodu sonuna tahminî harf eklenmedi. Yeni şema/anahtar davranışı ekip kararı gerektirir. Eski PDF çizimindeki zorunlu teaching assignment üzerinden review ilişkisi güncel optional OfferingId modeliyle birlikte okunmalıdır. Bu belgede başka bir ekip üyesinin kodu, gereksinim taslağı veya eski şema çizimi değiştirilmedi.
