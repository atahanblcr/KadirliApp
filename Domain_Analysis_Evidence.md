# KadirliApp: Neden Rich Domain Model ve Domain Events Bir "Tercih" Değil, Zorunluluktur?

Bu belge, projede uygulanan Anemik Domain (Anemic Domain Model) yapısının bir "kodlama stili tercihi" olmadığını; aksine veri bütünlüğünü tehdit eden ve geçmişte **canlı (production) ortamında sessiz hasarlara yol açmış** yapısal bir mimari zafiyet olduğunu kanıtlamak için hazırlanmıştır.

## Kanıt 1: Anemik Domain'in Yol Açtığı Sessiz Veri Bozulmaları (Faz 12.10)

`Active_Context.md` Faz 12.10 kayıtlarına göre, moderasyon sürecinde (eski Düzenle formu üzerinden) şöyle bir canlı ortam hatası yaşanmıştır:

> *"Canlı Postgres'te doğrulandı: süresi dolmuş bir ilan bu yoldan `approved` yapılınca `ExpiresAt` geçmişte kalıyor, `ApprovedBy` NULL oluyor ve vatandaş ilanı göremiyordu."*

**Neden Oldu?**
Çünkü `Ad` sınıfı şu an bir **veri torbasıdır (Anemic Domain)**:
```csharp
// Mevcut Ad.cs (Anemik)
public string Status { get; set; } = "pending";
public Guid? ApprovedBy { get; set; }
public DateTime ExpiresAt { get; set; }
```
Bu yapıda `Status` property'sine `"approved"` yazan herhangi bir kod bloğu (örn: bir controller veya eksik yazılmış bir Command Handler), `ApprovedBy` alanını doldurmaya veya `ExpiresAt` süresini uzatmaya **zorlanmaz**. Sınıf kendi iç tutarlılığını (invariant) koruyamaz. Sistemdeki her Command Handler, bu iş kuralını *hatasız bir şekilde ezbere bilmek ve tekrar yazmak* zorundadır.

**Rich Domain Model (Zengin Domain) Olsaydı Ne Olurdu?**
Property'ler dışarıdan doğrudan değiştirilemez (private set) olurdu:
```csharp
// Olması Gereken Ad.cs (Rich Domain)
public string Status { get; private set; } = "pending";
public Guid? ApprovedBy { get; private set; }

public void Approve(Guid approvedBy)
{
    if (ExpiresAt < DateTime.UtcNow) 
        throw new DomainException("Süresi dolmuş ilan onaylanamaz!");
        
    Status = "approved";
    ApprovedBy = approvedBy;
    ApprovedAt = DateTime.UtcNow;
    
    // Domain Event Fırlat
    AddDomainEvent(new AdApprovedEvent(this));
}
```
Eğer Zengin Domain Model kullanılsaydı, **derleyici (compiler) seviyesinde** o hatanın yapılması imkansız olurdu. Geliştirici sadece `ad.Approve(adminId)` metodunu çağırabilir, kalan tüm doğrulamaları `Ad` entity'sinin kendisi yapardı. Bu bir tercih değil, **hata yapmayı imkansız kılan** bir mimari zırhtır.

## Kanıt 2: Domain Events Eksikliğinin Yol Açtığı Kod Tekrarı ve Unutulan Yan Etkiler

`Active_Context.md` kayıtlarında yer alan bir diğer bulgu:

> *"UpdateMyAdCommandHandler — yani vatandaşın kendi ilanını düzenlemesi — durumu `pending`'e çekip onay/red izlerini elle temizliyordu. Approve/Reject'teki aynı bilginin üçüncü kopyası: ilana yarın bir onay izi alanı eklendiğinde iki yer güncellenip üçüncüsü unutulur..."*

**Neden Oldu?**
Olay Güdümlü (Event-Driven) mimari olmadığı için. Bir ilanın durumu değiştiğinde yapılması gereken "yan etkiler" (izleri temizleme, bildirim atma, log yazma) her Command Handler'ın içine prosedürel olarak kopyala-yapıştır yapılmıştır.

**Domain Events Olsaydı Ne Olurdu?**
`Ad` entity'si içinde `ad.Update(...)` metodu çağrıldığında entity sadece bir `AdUpdatedEvent` fırlatırdı. 
Sistemin tek bir yerindeki `AdUpdatedEventHandler`, bu event'i dinleyip onay izlerini temizlerdi. Gelecekte ilanın düzenlenmesine yeni bir yan etki ekleneceği zaman, mevcut 3 farklı Command Handler'ı değiştirmek yerine sadece event handler'a yeni bir kural eklenirdi (Open/Closed Principle).

## Kanıt 3: DevOps, CD ve IaC (Altyapı Olarak Kod) Eksikliği Neden Puan Düşürüyor?

Sistem kodu (Application/API) kusursuz işliyor olabilir ancak Faz 12.2b'deki şu not çok kritiktir:
> *"Panelin admin parolası bu makinede bilinmiyor... panel oturumunu kullanıcı açtı."*

Sürekli Dağıtım (Continuous Deployment - CD) boru hatları ve Terraform gibi IaC (Infrastructure as Code) araçları olsaydı;
1. Ortam değişkenleri (secrets, admin parolaları) güvenli ve otomatik bir şekilde deploy edilirdi.
2. Production ortamının replikası, tek bir tuşla veya script ile (Terraform apply) oluşturulabilirdi. Şu anda uygulamanın production altyapısı bir makinedeki manuel Docker Compose süreçlerine bağımlı. Bu "çalışan bir ürün" için yeterli olsa da, projede hedeflenen **"Sektör Standartlarını Belirleyen Enterprise"** seviyesi için bir eksi puandır.

## Sonuç
Gemini'nin Domain Katmanı (7.5/10) ve DevOps (8.5/10) puanları, kodun "çalışmaması" veya "kötü olması" nedeniyle değil; projenin mevcut **yüksek standartlarına (CQRS, Testcontainers, SweepTests) ayak uyduramayan** mimari engeller barındırmasından verilmiştir. Projenin 10/10 bir şahesere dönüşmesi için bu engellerin aşılması matematiksel bir gerekliliktir.
