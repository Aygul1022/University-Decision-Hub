# UniversityDecisionHub ajan devir komutları

16 Kasım 2026 durum kaydı. Bu metin model yeteneklerini aktaramaz; proje bağlamını, çalışma kurallarını ve doğrulama ölçütlerini korur. Yeni ajan mevcut dosyaları kontrol etmelidir; bu kayıt gelecekte güncelliğini kaybedebilir.

## Her yeni ajana ilk mesaj

```text
UniversityDecisionHub üzerinde Aygül'e Türkçe mentor olarak yardım et. Önce AGENTS.md, git status ve ilgili mevcut kodu oku. Mevcut dosya/klasör yapısını koru; küçük, göreve odaklı değişiklikler yap. Yeni mimari, kapsam genişletme, toplu refactor veya örnek veri ekleme.

Kullanıcının bu görev için verdiği yetki sınırında çalış. Okuma/incelemeye devam et; uygulama değişikliği istenmediyse kendiliğinden kod değiştirme. DB aktarımı, mevcut veri güncellemesi, migration uygulaması ve push için açık görev yetkisi gerekir. Push'u Aygül yapacak. Teammate mesajlarını sadece taslak olarak ver.

Her ekip üyesinin PostgreSQL veritabanı yereldir. Sayısal ID'leri bilgisayarlar arasında paylaşılmış kimlik sayma; sabit aktarım anahtarlarını kullan. Şifreleri, JWT anahtarlarını, öğrenci verilerini veya ham veri dosyalarını çıktıya/Git'e koyma. User Secrets içeriğini yazdırma.

Önce ilgili DTO, entity, EF mapping, migration snapshot ve import kodunu birlikte incele. Yanlış testi geçsin diye beklentiyi kaldırma; model uyuşmazlığını gizleme. Eski migration'ları veya snapshot'ı körlemesine yeniden yazma. Yeni migration gerekiyorsa oluşturulan değişiklikleri veri kaybı açısından incele; uygulamadan önce etkisini anlat.

Her değişikliğin nedenini ve hangi dosyayı etkilediğini anlaşılır biçimde açıkla. Yalnızca ilgili kontrolleri çalıştır. Derleme, dosya doğrulaması, SQLite kontrolü, gerçek PostgreSQL testi ve CI raporunu birbirinden ayır. Çalıştırmadığın testi geçmiş sayma. Başarısızsa sebebi ve kalan işi belirt.

Implementation/fix/migration sonrası university-implementation-log becerisiyle Doc/Implementation_Log.docx güncelle. Test sonucu geldiğinde university-test-log ile Doc/Backend_Test_Log.docx güncelle; kullanıcı/ekip bildirimi ile kendi çalıştırdığın sonucu ayır. Mevcut kayıtları koru. Beceriler yoksa Doc/Skills altındaki proje kopyalarını oku.

Şimdi aşağıdaki tek görevi yap; sonunda değişiklikleri, kanıtları ve sıradaki tek adımı söyle.
```

## Korunacak proje kararları

- Faculty, Department, Programme ayrıdır. Dil/derece farklılıkları Programme düzeyinde korunur.
- FacultyKey, DepartmentKey, ProgrammeKey, InstructorKey, CourseKey ve CurriculumKey sabittir; isim değişince yeniden numaralandırılmaz.
- Resmî DepartmentCode bilinmiyorsa null; benzersiz olmak zorunda değildir. Unknown yalnızca gösterim metnidir.
- Eksik kredi/AKTS/sınıf/seçmelilik bilgisi null kalır; sıfır veya false tahmin edilmez. ANS 107 için doğrulanmış isElective=false korunur.
- Dersin programa/müfredata özgü bilgileri ProgrammeCourse'tadır. Course bölüm ilişkisi pilotta doğrulanmış programdan türetilmiş katalog bağlamıdır; bağımsız sorumlu bölüm doğrulaması diye sunulmaz.
- Yorum uygunluğu kayıtlı programın herhangi bir müfredatındaki derslere dayanır; tarihî ve seçmeli dersler dahil. Aynı bölüm şartı veya aktif dönem şartı yoktur. Bilinen sınıf koşulu kontrol edilir; bilinmeyen ders sınıfı tek başına engellemez. Çelişen sınıf verisini sessizce çözme.
- Öğrenci tüm öğretim elemanlarından seçebilir. Yıl/dönem ve dersi alma beyanı yorum bağlamıdır; resmî görevlendirme üretmez.
- CourseOffering/OfferingInstructor kaynak veri yokken boş kalır. Yorum için zorunlu değildir.
- Mevcut bir kullanıcı/bir ders benzersizliğini yeni açık karar olmadan değiştirme.
- Kayıtta programı zorunlu yapma veya tüm bölüm programlarına otomatik hak verme. Mevcut profil/yorum sözleşmesini incele; değişiklik gerekiyorsa önce açıkla.

## Görev 1 Güncel JSON hazırlama

```text
Datas2 Excel paketini incele ve mevcut CatalogImport v2 sözleşmesine uygun YENİ bir JSON hazırla. Kaynak: C:/Users/aygul/OneDrive/Desktop/Datas2. Önce Doc/Catalog_V2_Contract.md ve Features/CatalogImport kodunu oku; spreadsheets becerisini kullan.

Önceki temel JSON: C:/Users/aygul/OneDrive/Desktop/Datas/catalog.ready.json. Son pilot: private-data/catalog.pilot.v2.json. Bunları ve kaynak Excel'leri üzerine yazma. Çıktıyı private-data/catalog.ready.datas2.json olarak ayrı oluştur; var ise sessizce değiştirme.

Faculty.xlsx, Departments.xlsx, Programmes.xlsx, Instructors.xlsx ve Course_Pilot_Anestezi.xlsx kullan. Course_Staging.xlsx içindeki 5442 satırı otomatik olarak Course'a dönüştürme. Pilotun Course ve ProgrammeCourse sayfalarını course_key üzerinden eşleştir; sayısal kredi/AKTS ve boolean/null tiplerini koru. Kaynak notlarını API'nin kabul etmediği alanlar olarak JSON'a ekleme, Excel'de koru.

schemaVersion=2 ve altı koleksiyon eksiksiz olsun. Beklenen ilk kontrol sayıları: 9 faculty, 130 department, 92 programme, 452 instructor, 5 course, 5 programmeCourse. Sayıları dosyadan doğrula. Anahtar benzersizliği, parent ilişkileri, alan uzunlukları, null değerler ve ANS107 false değerini kontrol et. EEM 2025 verisini 2026 diye etiketleme.

Önceki v2 pilotla alan farklarını raporla. 16 Kasım ilk dört koleksiyon karşılaştırmasında sadece DEP-0086 ve instructorKey 396/397/398 eklendi; eski satır değişikliği/silinmesi görülmedi. Bu gözlemi yeni dosyada tekrar doğrula; pilot ve staging için doğrulanmış sayma.

Sonra yalnız validate-catalog komutunu mutlak JSON yolu ile çalıştır. DB bağlantısı/aktarımı/migration yapma. JSON'un gitignored olduğunu kontrol et. Çıktıyı ve gerçek doğrulama sonucunu raporla.
```

## Görev 2 Aktarım için mentor kontrolü

```text
Doğrulanmış catalog.ready.datas2.json için yerel aktarım adımlarını hazırla. Önce import kodunun güncel davranışını kontrol et: mevcut anahtarda değişen alanlar tüm aktarımı reddediyor; otomatik update/delete yok. JSON doğrulaması gerçek DB'nin uyumlu olduğunu kanıtlamaz.

Mevcut DB'de önceki 679 katalog kaydı ve 10 pilot kaydı ile ANS107 düzeltmesi gerçekten varsa yalnız dört yeni kayıt beklenir; bunu gözlenen sonuç gibi yazma. Migration durumunu ve hedefin yerel UniversityDecisionHub olduğunu şifre yazdırmadan doğrulat. Veri farklılığı varsa silme/reimport yerine dar bir uzlaştırma önerisi sun.

Aygül'e adım adım komutları ver; DB yazısını kendin çalıştırma. --apply öncesi beklenen ekleme/değişiklik etkisini anlat. Sonuç gelince sayıları ve yeniden aktarımda değişmeden kalan kayıtları değerlendir, test günlüğünü güncelle. CourseOffering/OfferingInstructor oluşturma.
```

## Görev 3 Ozan ve Sami entegrasyonunu kontrol etme

```text
Yalnız inceleme yap. Yerel dal/commit ve mevcut test projelerini belirle. Ozan'ın feature/ozan-catalog-pages ec1b839 CI başarısı ekip tarafından bildirildi; yerelde merge edilmiş sayma. Mevcut .github/workflows ve test projelerini varsa oku. Dal değiştirme/merge/pull/push yapma.

Auth profil alanları, Programme seçimi, Course detail -> review link parametreleri, review POST DTO, review okuma/filtre/sayfalama ve yetkilendirme sözleşmelerini karşılaştır. Hangi endpoint/dosyanın gerçekten var olduğunu kanıtla. Uyuşmazlıkları dosya ve satırla belirt; sadece gerekli küçük değişiklikleri öner. Uydurma endpoint veya alan adı kullanma.
```

## Görev 4 Onaylı kod değişikliği ve gerekli testler

```text
Yalnız aşağıda tanımlanan/onaylanan değişikliği uygula: [BURAYA TEK SOMUT GÖREV]. İlgisiz kodu değiştirme. Önce etkilenen akışı ve mevcut testleri incele, sonra en küçük düzeltmeyi yap.

API'yi derle; değişiklikle ilgili mevcut kontrol projelerini çalıştır. Şu an bilinen projeler Tests/UniversityDecisionHub.CourseChecks ve Tests/UniversityDecisionHub.AccountChecks. Yeni dallarda diğer testler bulunabilir; keşfetmeden isim/başarı uydurma. Model değiştiyse pending-model-change kontrolü ve migration içeriği incelemesi yap. Gerçek PostgreSQL migration/import uygulamasını ayrı yetki olmadan yapma.

Sonuçları komut/exit code ve kapsamla kaydet. Testin yanlış nedenle geçmediğini değerlendir. Mevcut proje günlüklerini güncelle. Commit/push yapma. Sonunda değiştirdiğin dosyaları, doğrulanan davranışı, test edilemeyen kısmı ve gereken kullanıcı adımını söyle.
```

## Görev 5 Pilot üzerinde uçtan uca kullanıcı testi

```text
Mevcut frontend ve API rotalarını okuyarak Aygül'e tek tek uygulanabilir pilot test adımları ver: giriş -> gerekirse mevcut profil akışıyla programme seçimi -> Anestezi ders detayı -> yorum formu -> öğretim elemanı/yıl/dönem/beyan -> gönderim -> yorum okuma.

Gerçek öğrenci hesabı veya yorumunu kendiliğinden oluşturma/silme. Önce kullanıcının onayladığı yerel test hesabı ve test verisini belirle. Başarı senaryosu ve ilgili kritik ret senaryolarını seç: kimliksiz erişim, program dışı ders, eksik beyan, mevcut politika altında ikinci yorum. Testleri sadece mevcut sözleşmeye göre beklenen HTTP durumlarıyla yaz. Kullanıcı çıktısı geldikçe kanıtı kaydet; formun açılmasını bütün akışın geçtiği sayma.
```

## Sonraki ajan için durum özeti

Temel katalog ve pilotun yerel aktarımı kullanıcı tarafından bildirildi. ANS107 SQL düzeltmesi kullanıcı tarafından doğrulandı; gerçek DB bu devir sırasında okunmadı. Datas2 incelemesi dosya düzeyindedir. Ozan'ın CI bildirimi ayrı dal için geçerlidir. Yeni Datas2 JSON'u bu devir belgesi hazırlanırken henüz üretilmedi. Sıradaki görev Görev 1'dir.
