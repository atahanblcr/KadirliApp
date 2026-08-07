using Microsoft.AspNetCore.Authorization;
using KadirliApp.Web.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Application.Features.PushCampaigns.Queries;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using KadirliApp.Web.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace KadirliApp.Web.Controllers;

[Authorize(Roles = "admin,super_admin,moderator")]
[PanelPermission("power-outages")]
public class PowerOutagesAdminController : Controller
{
    /// <summary>Index süzgecinde "sözlükle eşleşmemiş kayıtlar" seçeneği.</summary>
    public const string UnmatchedNeighborhoodKey = "unmatched";

    private readonly ISender _sender;
    private readonly IUnitOfWork _uow;

    public PowerOutagesAdminController(ISender sender, IUnitOfWork uow)
    {
        _sender = sender;
        _uow = uow;
    }

    private const int PageSize = 20;

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? neighborhood,
        [FromQuery] string? neighborhoodId,
        [FromQuery] string? phase,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1)
    {
        var result = await _sender.Send(new GetPowerOutagesQuery());
        var outages = result.Success ? result.Data ?? new List<PowerOutageDto>() : new List<PowerOutageDto>();

        // ⚠️ Sayfalama BİLİNÇLİ olarak bellekte yapılıyor: GetPowerOutagesQuery tarih filtresi
        // olmadan TÜM kayıtları döner ve mobil (Faz 11.4) süren/planlı ayrımını istemcide bu tam
        // listeye bakarak yapıyor. Sorguyu PagedResult'a çevirmek public kontratı kırardı.
        // Panelin tek sorunu 1000 satırı ekrana basmaktı — çözülen o.
        //
        // Faz 11.17: süzgeç de aynı sebeple bellekte. Uca filtre parametresi eklemek
        // görünmez sözleşme #1'i (sayfalamayan düz dizi) tartışmaya açardı.
        var now = DateTime.UtcNow;
        var wantedPhase = PowerOutagePhaseRules.Parse(phase);

        var filtered = outages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(neighborhood))
            filtered = filtered.Where(o => o.Neighborhood != null &&
                o.Neighborhood.Contains(neighborhood.Trim(), StringComparison.OrdinalIgnoreCase));

        // Faz 12.3: sözlük bazlı süzgeç. Serbest metin araması KALDI — yıllardır kayıtlı
        // eşleşmemiş satırlar yalnız metinle bulunabiliyor ve o kayıtlar tam olarak
        // yöneticinin düzeltmesi gereken kayıtlar.
        if (string.Equals(neighborhoodId, UnmatchedNeighborhoodKey, StringComparison.Ordinal))
            filtered = filtered.Where(o => o.NeighborhoodId is null);
        else if (Guid.TryParse(neighborhoodId, out var wantedNeighborhood))
            filtered = filtered.Where(o => o.NeighborhoodId == wantedNeighborhood);

        if (wantedPhase is { } p)
            filtered = filtered.Where(o => PowerOutagePhaseRules.Phase(o.StartTime, o.EndTime, now) == p);

        // Tarih aralığı **kesişim** üzerinden: "1–3 Ağustos" seçen yönetici, 31 Temmuz'da
        // başlayıp 2 Ağustos'ta biten kesintiyi de görmek ister. Yalnız StartTime'a bakmak
        // uzun kesintileri sessizce eler.
        if (from is { } f)
        {
            var start = DateTime.SpecifyKind(f.Date, DateTimeKind.Utc);
            filtered = filtered.Where(o => o.EndTime >= start);
        }

        if (to is { } t)
        {
            var end = DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Utc);
            filtered = filtered.Where(o => o.StartTime < end);
        }

        ViewBag.Neighborhood = neighborhood;
        ViewBag.NeighborhoodId = neighborhoodId;
        ViewBag.Phase = phase;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Now = now;
        ViewBag.TotalBeforeFilter = outages.Count;

        // 🔑 "Mahallesi eşleşmemiş" sayacı — geri doldurmanın raporunun ekrandaki karşılığı.
        // Bu kayıtlar bildirim GÖNDEREMEZ; sayı görünmezse yönetici neden bazı kesintilerde
        // bildirim kutusunun kapalı olduğunu hiçbir zaman anlamaz.
        ViewBag.UnmatchedCount = outages.Count(o => o.NeighborhoodId is null
            && !string.IsNullOrWhiteSpace(o.Neighborhood));

        await LoadNeighborhoodsAsync();

        return View(Paginate(filtered.ToList(), page));
    }

    private static PagedResult<PowerOutageDto> Paginate(List<PowerOutageDto> source, int page)
    {
        var (currentPage, pageSize) = Pagination.Clamp(page, PageSize, Pagination.AdminMaxLimit);

        return new PagedResult<PowerOutageDto>
        {
            Items = source.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = source.Count,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadNeighborhoodsAsync();
        return View(new CreatePowerOutageDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePowerOutageDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadNeighborhoodsAsync();
            return View(dto);
        }

        var command = new KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage.CreatePowerOutageCommand
        {
            Dto = dto,
            CreatedBy = GetAdminId()
        };

        var result = await _sender.Send(command);
        if (result.Success)
        {
            TempData["Success"] = "Elektrik kesintisi başarıyla eklendi." + NotificationSuffix(command.NotifyOutcome, command.NotifiedCount);
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Kesinti eklenirken bir hata oluştu.";
        await LoadNeighborhoodsAsync();
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _sender.Send(new KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById.GetPowerOutageByIdQuery { Id = id });
        if (!result.Success || result.Data == null) return NotFound();

        var dto = new UpdatePowerOutageDto
        {
            Neighborhood = result.Data.Neighborhood,
            NeighborhoodId = result.Data.NeighborhoodId,
            AreaDetail = result.Data.AreaDetail,
            StartTime = result.Data.StartTime,
            EndTime = result.Data.EndTime,
            Reason = result.Data.Reason
        };

        // 🔑 Bildirim zaten gönderildiyse görünüm bunu SUNUCUDAN öğrenir ve kutuyu
        // "gönder" yerine "gönderildi" olarak çizer. Görünüm kendi koşulunu yazsaydı
        // komutun yok sayacağı bir buton çizilirdi (12.2b'nin `CanCancel` dersi).
        ViewBag.AlreadyNotified = result.Data.AnnouncementId is not null;
        ViewBag.AnnouncementId = result.Data.AnnouncementId;

        await LoadNeighborhoodsAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, UpdatePowerOutageDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadNeighborhoodsAsync();
            return View(dto);
        }

        var command = new KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage.UpdatePowerOutageCommand
        {
            Id = id,
            Dto = dto,
            UpdatedBy = GetAdminId()
        };

        var result = await _sender.Send(command);
        if (result.Success)
        {
            TempData["Success"] = "Elektrik kesintisi başarıyla güncellendi." + NotificationSuffix(command.NotifyOutcome, command.NotifiedCount);
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error?.Message ?? "Kesinti güncellenirken bir hata oluştu.";
        await LoadNeighborhoodsAsync();
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _sender.Send(new DeletePowerOutageCommand { Id = id });
        if (result.Success)
        {
            TempData["Success"] = "Kesinti bilgisi silindi. Varsa duyurusu ve bildirimleri de kaldırıldı.";
        }
        else
        {
            TempData["Error"] = result.Error?.Message ?? "Silinirken bir hata oluştu.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Formun canlı "kaç kişiye gidecek?" önizlemesi (AJAX).
    /// </summary>
    /// <remarks>
    /// 🔴 Hesap <c>INotificationDispatcher</c>'a devredilir (12.2b'nin sorgusu, aynı sabit).
    /// Panel kendi <c>COUNT</c>'unu yazsaydı önizleme "342 kişiye gidecek" der, gönderim 280
    /// satır yazardı ve fark <b>hiçbir yerde</b> görünmezdi — görünmez sözleşme #38.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> EstimateRecipients([FromQuery] Guid[]? neighborhoodIds)
    {
        var ids = (neighborhoodIds ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();

        // ⚠️ Mahalle seçilmemişse "herkes" DEĞİL, "kimse". Kesinti bildirimi her zaman
        // mahalle hedeflidir; boş listeyi "tüm şehir" saymak bir form hatasını 40 bin
        // kişiye giden bildirime çevirirdi (dispatcher'daki null ≠ boş liste ayrımı).
        if (ids.Length == 0) return Json(new { count = 0 });

        var count = await _sender.Send(new EstimatePushRecipientsQuery(PushTargetTypes.Neighborhood, ids));
        return Json(new { count });
    }

    private async Task LoadNeighborhoodsAsync()
    {
        ViewBag.Neighborhoods = await _uow.Repository<Neighborhood>().Query()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    private Guid? GetAdminId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>
    /// 🔑 Mesaj <b>sayıyı söyler</b> ve gönderilmediyse <b>neden</b> gönderilmediğini söyler.
    /// "Kesinti eklendi" deyip susmak, hedeflemeye kimse uymadığında yöneticiye gitmeyen bir
    /// bildirimi gitmiş sandırırdı — bu fazın savaştığı sessiz hasar sınıfı (12.2b ile aynı karar).
    /// </summary>
    private static string NotificationSuffix(PowerOutageNotifyOutcome outcome, int count) => outcome switch
    {
        PowerOutageNotifyOutcome.Created when count > 0 =>
            $" Bildirim {count} kişiye yazıldı — teslim durumu Bildirim Gönderimleri ekranından izlenebilir.",
        PowerOutageNotifyOutcome.Created =>
            " Seçilen mahallelerde bildirim alacak kayıtlı kullanıcı bulunamadı — hiç bildirim yazılmadı.",
        PowerOutageNotifyOutcome.NotTargetable =>
            " Bildirim gönderilemedi: kayda sözlükten bir mahalle seçilmemiş.",
        PowerOutageNotifyOutcome.Updated =>
            " Bu kesintinin duyurusu güncellendi — ikinci bir bildirim gönderilmedi.",
        _ => string.Empty
    };
}
