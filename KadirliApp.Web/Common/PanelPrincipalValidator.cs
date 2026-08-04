using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.18 — **panel oturumunun her istekte tazelenmesi** (11.15c C grubu).
///
/// 🔴 Kapatılan açık: çerez <c>ExpireTimeSpan = 8 saat</c> ile veriliyordu ve
/// <c>OnValidatePrincipal</c> yoktu. Yani personel **silinse, banlansa, pasife
/// alınsa ya da rolü düşürülse bile elindeki oturum 8 saat boyunca çalışmaya devam
/// ediyordu**; parola değiştirmek de açık oturumları düşürmüyordu. 11.15c oturumunda
/// canlıda gözlemlendi: önceki oturumdan kalan moderatör çerezi panele hâlâ giriyordu.
/// "Yetkiyi geri aldım" diyen yönetici aslında hiçbir şey geri almamış oluyordu.
///
/// 🔑 **Rol değişimi oturumu düşürmez, TAZELER.** Rolü düşürülen bir moderatörü dışarı
/// atmak yerine çerezindeki rol talebini güncellemek doğrusu: kullanıcı çalışmaya devam
/// eder ama artık **yeni** rolüyle eder. Atsaydık, rol yükseltmesi de (moderator → admin)
/// kullanıcıyı sebepsizce dışarı atardı.
///
/// ⚠️ Maliyet: kimliği doğrulanmış istek başına **tek** kullanıcı sorgusu. Panelin izin
/// süzgeci (<c>PanelMenuProvider</c>) zaten her istekte DB'ye gidiyor; ek yük marjinal.
/// Statik dosyalar <c>UseAuthentication</c>'dan ÖNCE servis edildiği için bu yolu hiç görmez.
/// </summary>
public static class PanelPrincipalValidator
{
    /// <summary>
    /// "Bu kullanıcı parolasını değiştirmek zorunda" bilgisinin taşıyıcısı.
    /// 🔑 Bilinçli olarak <b>claim değil</b> <c>HttpContext.Items</c>: claim olsaydı bayrak
    /// çerezde donar ve parola değiştikten sonra çerez yenilenene kadar yanlış kalırdı.
    /// Buradaki değer her istekte DB'den tazelenmiş satırdan gelir ve
    /// <see cref="RequirePasswordChangeFilter"/> onu **ek sorgu atmadan** okur.
    /// </summary>
    public const string MustChangePasswordItemKey = "panel.must_change_password";

    /// <summary>Panele girebilen roller — <c>AccountController.Login</c> ile aynı liste olmak zorunda.</summary>
    private static bool CanAccessPanel(UserRole role) =>
        role is UserRole.Admin or UserRole.SuperAdmin or UserRole.Moderator;

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity is not { IsAuthenticated: true })
            return;

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await RejectAsync(context);
            return;
        }

        var uow = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        // ⚠️ Global query filter (deleted_at IS NULL) burada bilerek KALDIRILMIYOR:
        // silinmiş kullanıcı zaten bulunamaz ve aşağıda oturumu düşer. IgnoreQueryFilters()
        // eklenseydi silinmiş personelin oturumu ayakta kalırdı — tam tersi sonuç.
        var user = await uow.Repository<User>().Query()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Role,
                u.IsActive,
                u.IsBanned,
                u.PasswordChangedAt,
                u.MustChangePassword
            })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        // Silinmiş · pasif · banlı · artık panele giremeyen rol → oturum biter.
        if (user is null || !user.IsActive || user.IsBanned || !CanAccessPanel(user.Role))
        {
            await RejectAsync(context);
            return;
        }

        // 🔑 Parola değişimi açık oturumları düşürür. Çerez parolanın değiştiği andan ÖNCE
        // düzenlenmişse artık geçersizdir — "şifremi değiştirdim" demek, çalınmış çerezin
        // de ölmesi demektir. (Kendi oturumu da düşer; ChangePassword akışı bu yüzden
        // hemen ardından yeniden giriş ister.)
        //
        // ⚠️ **Karşılaştırma SANİYEYE yuvarlanarak yapılır.** Çerezin düzenlenme anı
        // biletin içine RFC1123 biçiminde ("r") yazılır ve o biçim saniye altını
        // TAŞIMAZ — yani geri okunan değer daima aşağı yuvarlanmıştır. Ham
        // karşılaştırma yapıldığında parolasını değiştiren kişi **kendi** oturumundan
        // düşüyordu: damga 12:00:00.750 iken yeni çerezin okunan anı 12:00:00.000
        // görünüyor ve "parola çerezden sonra değişmiş" sanılıyordu. (Testle yakalandı.)
        if (user.PasswordChangedAt is { } changedAt &&
            context.Properties.IssuedUtc is { } issuedUtc &&
            TruncateToSecond(changedAt) > TruncateToSecond(issuedUtc.UtcDateTime))
        {
            await RejectAsync(context);
            return;
        }

        // Zorunlu parola değişimi bayrağını isteğe iliştir (filtre ek sorgu atmasın).
        context.HttpContext.Items[MustChangePasswordItemKey] = user.MustChangePassword;

        // Rol değiştiyse çerezdeki talebi tazele (oturumu düşürmeden).
        var currentRoleClaim = principal.FindFirstValue(ClaimTypes.Role);
        var actualRole = user.Role.ToRoleString();
        if (!string.Equals(currentRoleClaim, actualRole, StringComparison.Ordinal))
        {
            var identity = new ClaimsIdentity(
                principal.Claims.Where(c => c.Type != ClaimTypes.Role).Append(new Claim(ClaimTypes.Role, actualRole)),
                CookieAuthenticationDefaults.AuthenticationScheme);

            context.ReplacePrincipal(new ClaimsPrincipal(identity));
            context.ShouldRenew = true;
        }
    }

    /// <summary>Çerezin taşıyabildiği en ince zaman birimi saniyedir; iki taraf da oraya indirilir.</summary>
    private static DateTime TruncateToSecond(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
