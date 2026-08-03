using System.Net;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15b — **panelden yazılan gerçekten kaydediliyor mu?**
///
/// Sayfanın 200 dönmesi hiçbir şey kanıtlamaz: panelde bir form, alan adı değiştiği için
/// modele bağlanmayabilir, komut sessizce boş kayıt yazabilir ya da yönlendirme başarı
/// mesajı gösterirken hiçbir satır oluşmamış olabilir. Buradaki testler **veritabanına
/// bakar**.
///
/// Her test kendi verisini benzersiz bir işaretçiyle kurar ve sonunda temizler
/// (test veritabanı yalnız <c>DbSeeder</c> lookup verisiyle gelir — 11.14 dersi).
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelMutationTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "PanelTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelMutationTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Neighborhoods.IgnoreQueryFilters().Where(n => n.Name.Contains(_marker)).ExecuteDeleteAsync();
            await db.GuideItems.IgnoreQueryFilters().Where(g => g.Name.Contains(_marker)).ExecuteDeleteAsync();
            await db.GuideCategories.IgnoreQueryFilters().Where(g => g.Name.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task<T?> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T? result = default;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    // ─────────────────────────── Tanımlar (lookups) ───────────────────────────

    /// <summary>
    /// Mahalle, uygulamanın en çok referans verilen sözlüğü (kullanıcı profili, vefat
    /// ilanı, kesinti filtresi). Panelden eklenemezse mobilde kimse mahallesini seçemez.
    /// </summary>
    [Fact]
    public async Task NeighborhoodCreate_PersistsAndAppearsInTheList()
    {
        var client = await _factory.SuperAdminAsync();
        var name = _marker + " Mahallesi";

        var response = await client.PostFormAsync("/LookupsAdmin/NeighborhoodCreate",
            new Dictionary<string, string> { ["name"] = name, ["type"] = "mahalle", ["displayOrder"] = "5" },
            tokenFromPath: "/LookupsAdmin/Index");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "başarılı kayıt listeye yönlendirir");

        var saved = await QueryDbAsync(db => db.Neighborhoods.FirstOrDefaultAsync(n => n.Name == name));
        saved.Should().NotBeNull("panelden eklenen mahalle veritabanında olmalı");
        saved!.DisplayOrder.Should().Be(5, "form alanı modele bağlanmalı");
        saved.Slug.Should().NotBeNullOrWhiteSpace("slug üretilmezse mobil kayıt eşleştirmesi bozulur");

        var list = await (await client.GetAsync("/LookupsAdmin/Index")).ReadDecodedBodyAsync();
        list.Should().Contain(name, "kaydedilen mahalle listede görünmeli");
    }

    /// <summary>
    /// Benzersizlik kuralı panelde de geçerli olmalı: aynı adla ikinci mahalle,
    /// mobil tarafta iki özdeş seçenek üretir ve kullanıcı hangisini seçtiğini bilemez.
    /// </summary>
    [Fact]
    public async Task NeighborhoodCreate_RejectsDuplicateName()
    {
        var client = await _factory.SuperAdminAsync();
        var name = _marker + " Tekrar";
        var fields = new Dictionary<string, string> { ["name"] = name, ["displayOrder"] = "1" };

        await client.PostFormAsync("/LookupsAdmin/NeighborhoodCreate", fields, "/LookupsAdmin/Index");
        await client.PostFormAsync("/LookupsAdmin/NeighborhoodCreate", fields, "/LookupsAdmin/Index");

        var count = await QueryDbAsync(db => db.Neighborhoods.CountAsync(n => n.Name == name));
        count.Should().Be(1, "aynı adla ikinci mahalle oluşmamalı");
    }

    /// <summary>
    /// Lookup mutasyonları <c>IAuditableCommand</c> — "bu mahalleyi kim ekledi?" sorusu
    /// cevaplanabilir olmalı. Audit izi sessizce düşerse denetim kaydı yalan söyler.
    /// </summary>
    [Fact]
    public async Task NeighborhoodCreate_LeavesAnAuditTrail()
    {
        var client = await _factory.SuperAdminAsync();
        var name = _marker + " İzli";

        await client.PostFormAsync("/LookupsAdmin/NeighborhoodCreate",
            new Dictionary<string, string> { ["name"] = name, ["displayOrder"] = "1" }, "/LookupsAdmin/Index");

        // ⚠️ `audit_logs.details` kolonu **jsonb**; LINQ'te `.Contains()` yazmak
        // `like_escape(jsonb, unknown)` hatasıyla patlar. Süzme belleğe alındıktan sonra.
        var audits = await QueryDbAsync(db => db.AuditLogs
            .Where(a => a.Module == "lookups" && a.Action == "create-neighborhood")
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync());

        var audit = audits!.FirstOrDefault(a => a.Details is not null && a.Details.Contains(name));

        audit.Should().NotBeNull("panelden yapılan lookup değişikliği audit_logs'a düşmeli");
        audit!.UserId.Should().NotBeEmpty("iz, işlemi yapan yöneticiye bağlanmalı");
        audit.AffectedType.Should().Be("Neighborhood");
    }

    /// <summary>Güncelleme yolu ayrı bir komut — kaydetmediği hâlde başarı gösterebilir.</summary>
    [Fact]
    public async Task NeighborhoodUpdate_ChangesTheStoredRow()
    {
        var client = await _factory.SuperAdminAsync();
        var name = _marker + " Güncellenecek";

        await client.PostFormAsync("/LookupsAdmin/NeighborhoodCreate",
            new Dictionary<string, string> { ["name"] = name, ["displayOrder"] = "1" }, "/LookupsAdmin/Index");
        var created = await QueryDbAsync(db => db.Neighborhoods.FirstAsync(n => n.Name == name));

        var newName = _marker + " Güncellendi";
        await client.PostFormAsync("/LookupsAdmin/NeighborhoodUpdate", new Dictionary<string, string>
        {
            ["id"] = created!.Id.ToString(),
            ["name"] = newName,
            ["displayOrder"] = "9",
            ["isActive"] = "true"
        }, "/LookupsAdmin/Index");

        var updated = await QueryDbAsync(db => db.Neighborhoods.FirstOrDefaultAsync(n => n.Id == created.Id));
        updated!.Name.Should().Be(newName);
        updated.DisplayOrder.Should().Be(9);
        updated.Slug.Should().NotBe(created.Slug, "ad değişince slug da yeniden üretilmeli");
    }

    // ─────────────────────────── Rehber (tam CRUD turu) ───────────────────────────

    /// <summary>
    /// Rehber, panelin **tam turunu** temsil eder: kategori ekle → kayıt ekle → düzenle →
    /// sil. Zincirin herhangi bir halkası kopunca yönetici modülü kullanamaz.
    /// </summary>
    [Fact]
    public async Task GuideModule_SupportsTheFullCreateEditDeleteRound()
    {
        var client = await _factory.SuperAdminAsync();

        // 1) Kategori
        var categoryName = _marker + " Kategori";
        var categoryResponse = await client.PostFormAsync("/GuideAdmin/CategoryCreate", new Dictionary<string, string>
        {
            ["Name"] = categoryName,
            ["Slug"] = _marker.ToLowerInvariant() + "-kategori",
            ["DisplayOrder"] = "1"
        });
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var category = await QueryDbAsync(db => db.GuideCategories.FirstOrDefaultAsync(c => c.Name == categoryName));
        category.Should().NotBeNull("rehber kategorisi kaydedilmeli");

        // 2) Kayıt
        var itemName = _marker + " Rehber Kaydı";
        var createResponse = await client.PostFormAsync("/GuideAdmin/Create", new Dictionary<string, string>
        {
            ["CategoryId"] = category!.Id.ToString(),
            ["Name"] = itemName,
            ["Phone"] = "03281234567",
            ["Address"] = "Kadirli merkez",
            ["IsActive"] = "true"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var item = await QueryDbAsync(db => db.GuideItems.FirstOrDefaultAsync(g => g.Name == itemName));
        item.Should().NotBeNull("rehber kaydı veritabanına yazılmalı");
        item!.Phone.Should().Be("03281234567", "telefon alanı modele bağlanmalı — rehberin tek işlevi arama");

        // 3) Düzenleme
        var editedName = _marker + " Düzenlendi";
        var editResponse = await client.PostFormAsync($"/GuideAdmin/Edit/{item.Id}", new Dictionary<string, string>
        {
            ["Id"] = item.Id.ToString(),
            ["CategoryId"] = category.Id.ToString(),
            ["Name"] = editedName,
            ["Phone"] = "03287654321",
            ["IsActive"] = "true"
        }, tokenFromPath: $"/GuideAdmin/Edit/{item.Id}");
        editResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var edited = await QueryDbAsync(db => db.GuideItems.FirstOrDefaultAsync(g => g.Id == item.Id));
        edited!.Name.Should().Be(editedName);
        edited.Phone.Should().Be("03287654321");

        // 4) Silme.
        // ⚠️ `GuideItem` `ISoftDeletable` DEĞİL — silme fizikseldir ve geri alınamaz.
        // (Vefat/ilan gibi kullanıcı üretimi içerikler soft-delete'li; rehber kaydı
        // yönetici verisi olduğu için bilinçli olarak farklı. Bu ayrım burada
        // kilitleniyor ki biri "hepsi soft-delete sanıyordum" diyerek yanılmasın.)
        var deleteResponse = await client.PostFormAsync($"/GuideAdmin/Delete/{item.Id}",
            new Dictionary<string, string> { ["id"] = item.Id.ToString() },
            tokenFromPath: "/GuideAdmin/Index");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var afterDelete = await QueryDbAsync(db =>
            db.GuideItems.IgnoreQueryFilters().AnyAsync(g => g.Id == item.Id));
        afterDelete.Should().BeFalse("rehber kaydı silindiğinde satır tamamen kalkar");

        // Silme moderasyon izi bırakmalı — "bu kaydı kim sildi?" cevaplanabilir olmalı.
        var audit = await QueryDbAsync(db => db.AuditLogs
            .FirstOrDefaultAsync(a => a.Module == "guide" && a.Action == "delete" && a.AffectedId == item.Id));
        audit.Should().NotBeNull("geri alınamayan silme mutlaka iz bırakmalı");
    }

    /// <summary>
    /// Zorunlu alan boş gönderilince komut çalışmamalı ve kullanıcı formu geri görmeli.
    /// 302 dönerse "kaydedildi" izlenimi verir ama ortada kayıt yoktur.
    /// </summary>
    [Fact]
    public async Task GuideCreate_WithoutName_DoesNotWriteARow()
    {
        var client = await _factory.SuperAdminAsync();
        var before = await QueryDbAsync(db => db.GuideItems.IgnoreQueryFilters().CountAsync());

        var response = await client.PostFormAsync("/GuideAdmin/Create", new Dictionary<string, string>
        {
            ["CategoryId"] = Guid.Empty.ToString(),
            ["Name"] = ""
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect,
            "geçersiz form yönlendirme yapmamalı — yönetici kaydedildiğini sanır");

        var after = await QueryDbAsync(db => db.GuideItems.IgnoreQueryFilters().CountAsync());
        after.Should().Be(before, "geçersiz gönderim satır oluşturmamalı");
    }
}
