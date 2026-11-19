# Docker demo kurulumu

19 Kasım 2026. Paket mevcut çalışan demodan hazırlanır; katalog 37586c3 kaynaklı 104 programlı sürümdür. Bu paket ayrı bir PostgreSQL oluşturur. Aygül'ün yerel PostgreSQL hesaplarını, yorumlarını, yönetici yetkilerini veya özel bağlantı ayarlarını taşımaz.

## İlk kurulum

Docker Desktop Linux container modunda çalışmalıdır. ZIP'i açın; PowerShell'de Dockerfile ve compose.yaml bulunan UniversityDecisionHub-DockerDemo klasörüne geçin.

```powershell
powershell -NoProfile -File .\Scripts\Initialize-DockerDemo.ps1
docker compose config --quiet
docker compose up --build -d
docker compose ps -a
docker compose logs --tail 60 migrate catalogue app
```

İlk kurulum Microsoft .NET 10 ve PostgreSQL 17 image'larını, NuGet paketlerini internetten indirir. Önce PostgreSQL hazır olur, migration bundle tamamlanır, katalog importer çalışır, sonra API açılır. Migrate ve catalogue servislerinin Exited (0) olması normaldir; db ve app çalışır durumda olmalıdır. Catalogue hata verirse API başlamamalıdır.

Tarayıcı adresi: http://localhost:5160 . Eski localhost:5155 oturumuyla karıştırmayın. Bu, Sami'nin bilgisayarında ayrı bir demo olur; ekip için internet bağlantısı oluşturmaz. Servis portu yalnızca loopback'e açıktır. Development ayarı bu yerel demo içindir; internet yayınına hazır üretim yapılandırması değildir.

.env otomatik rastgele DB parolası ve sabit JWT anahtarı içerir; içerik terminale yazdırılmaz, ZIP veya Git'e eklenmez. Mevcut .env değiştirilmez. Container yeniden başlatıldığında bu dosya ve volume kaldığı sürece veriler/anahtar korunur. Volume varken DB parolasını kendiliğinizden değiştirmeyin.

## Katalog kontrolü

```powershell
docker compose exec -T db psql -U udh -d UniversityDecisionHub -c 'SELECT COUNT(*) FROM "Programmes"; SELECT COUNT(*) FROM "Courses"; SELECT COUNT(*) FROM "ProgrammeCourses";'
```

Beklenen: 104 program, 9346 ders, 12591 müfredat satırı. İlk tam katalog aktarımında 22641 kayıt eklenmesi beklenir. Yeniden aktarım aynı kayıtları atlar; mevcut farklı kayıtları güncellemez. Eski veya farklı bir volume çakışması varsa veriyi silmeyin, logları inceleyin.

## Kurgu hesaplar ve yönetici

Kayıt ekranından kurgu öğrenci ve kurgu yönetici hesaplarını oluşturun. Sonra sadece kayıtlı kurgu yönetici hesabını yetkilendirin:

```powershell
docker compose exec app dotnet UniversityDecisionHub.Api.dll make-admin "KAYITLI_KURGU_EPOSTA"
```

Yönetici hesabından çıkıp yeniden giriş yapın. Program/sınıf seçimi kaydetme ve yenileme, Bilgisayar Mühendisliği 175 dersin gelmesi, test yorumu gönderme ve başka hesapla okuma, yönetici gizleme/geri getirme ve kalıcı silme senaryolarını kurgu verilerle deneyin. Programında ders bulunmayan kullanıcıya ders uydurulmaz.

## Durdurma ve kalıcılık

```powershell
docker compose stop
docker compose start db app
```

İlk kurulum için start yerine up kullanılır. `docker compose down` container'ları kaldırır ama volume'u korur. `docker compose down -v` veriyi siler; normal kullanımda çalıştırmayın. Yerel asıl PostgreSQL bu Compose ortamına bağlı değildir.

## Paket üretme ve teslim

```powershell
powershell -NoProfile -File .\Scripts\Export-DockerDemo.ps1
```

Teslim dosyası private-data/UniversityDecisionHub-DockerDemo-2026-11-19.zip ve yanındaki SHA256 dosyasıdır. ZIP yalnızca gerekli uygulama kaynaklarını, frontend'i, Docker kurulum dosyalarını ve güncel katalog JSON'unu içerir. Word günlükleri, test çıktıları, .git, obj/bin, appsettings, User Secrets, .env, DB yedekleri ve gerçek kullanıcı verileri dahil edilmez. Mevcut ZIP üzerine yazılmaz; yeni çıktı için -OutputPath kullanılır.

Bu ZIP kaynak paketidir; alıcının ilk derleme için interneti olmalıdır. Offline image teslimi ancak image build ve testinden sonra ayrıca docker save ile yapılır. Test edilmemiş build, çalışıyor diye sunulmamalıdır.

## Kaynaklar

- Compose bağımlılıkları: https://docs.docker.com/compose/how-tos/startup-order/
- EF migration bundle: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying


## Git ile güncelleme

Git deposuyla kurulum yapan Sami, kendi değişikliklerini koruyup çalışma ağacının temiz olduğunu kontrol ettikten sonra:

```powershell
git fetch origin
git switch feature/docker-demo
git pull --ff-only origin feature/docker-demo
powershell -NoProfile -File .\Scripts\Initialize-DockerDemo.ps1
docker compose up --build -d
```

Yerel dal yoksa `git switch --track origin/feature/docker-demo` kullanılır. İlk kurulumda dal oluşturma ve çekme adımlarını buna göre uygulayın. Başka dalda commit edilmemiş çalışma varsa önce onu kaydedin; zorla dal değiştirmeyin. Güncelleme için `down -v` kullanmayın; mevcut Docker volume ve hesapları korunmalıdır. Yeni image çalışınca tarayıcıyı Ctrl+F5 ile yenileyin.

Yalnızca ZIP açarak kurulan klasör Git deposu değildir; git pull çalışmaz. Bu kurulumda güncel ZIP gerekir veya mevcut kurulumdan ayrı klasöre Git deposu klonlanır. Klasör değişse bile aynı Compose proje adı udh-demo mevcut volume'u kullanır. Mevcut .env ayarlarını paylaşmadan kendi bilgisayarınızda koruyun; çalışan volume varken yeni parola üretmeyin. Her bilgisayarda ayrı veritabanı bulunur; kod çekmek diğer bilgisayarın hesap/yorumlarını taşımaz.
