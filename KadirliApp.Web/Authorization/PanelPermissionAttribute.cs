using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KadirliApp.Web.Authorization;

/// <summary>
/// Faz 11.15b — **panelde izin matrisini yürürlüğe koyar.**
///
/// <para><b>Neden var:</b> Panel 10.9(e)'den beri "Moderatör (izin matrisine tabi)" rolüyle
/// personel oluşturabiliyor ve 16 modüllük bir izin matrisi sunuyordu. Ama panelin
/// <i>tüm</i> controller'ları <c>[Authorize(Roles = "admin,super_admin")]</c> taşıyordu:
/// oluşturulan moderatör panele girebiliyor, açılışta <c>/Dashboard/Index</c>'e
/// yönlendiriliyor ve **oradan itibaren her sayfada "Yetkiniz yok" görüyordu.** Matris
/// yalnız <c>/v1/admin/*</c> uçlarını (bugün hiçbir istemcinin kullanmadığı) etkiliyordu.
/// Yani panel, karşılığı olmayan bir yetki veriyordu.</para>
///
/// <para><b>Nasıl çalışır:</b> API tarafındaki <c>RequirePermissionAttribute</c> ile aynı
/// veri kaynağını (<see cref="IPermissionService"/> → <c>admin_permissions</c>) kullanır.
/// Fark: API'de her uca elle <c>(modül, eylem)</c> yazılır; panelde bu **aksiyon adından
/// türetilir**, çünkü 18 controller × ~8 aksiyon elle işaretlenirse bir tanesi mutlaka
/// unutulur ve o aksiyon sessizce korumasız kalır. Türetme yanılırsa daima <b>daha dar</b>
/// tarafa düşer (bilinmeyen POST = "update").</para>
///
/// <para>⚠️ Katman kuralı gereği <c>KadirliApp.Web</c>, <c>KadirliApp.Api</c>'ye referans
/// veremez — bu yüzden API'deki eşdeğer sınıf kopyalanmadı, panele özgü (aksiyondan
/// türetmeli) biçimde yeniden yazıldı.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PanelPermissionAttribute : TypeFilterAttribute
{
    public PanelPermissionAttribute(string module) : base(typeof(PanelPermissionFilter))
    {
        Arguments = new object[] { module };
    }
}

public sealed class PanelPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _module;
    private readonly IPermissionService _permissions;

    public PanelPermissionFilter(string module, IPermissionService permissions)
        => (_module, _permissions) = (module, permissions);

    /// <summary>
    /// Aksiyon adı → izin eylemi. Sıra önemli: "UpdateStatus" önce moderasyon olarak
    /// eşleşmeli, "Update" öneki olarak değil.
    /// </summary>
    internal static string ActionFor(string actionName, string httpMethod)
    {
        // Moderasyon kararları (onay/ret/doğrulama/yasaklama) tek yetkide toplanır:
        // "içeriği yayına alabilir mi?" sorusu, "düzenleyebilir mi?"den ayrı bir güvendir.
        if (StartsWithAny(actionName, "Approve", "Reject", "Verify", "Unverify", "Ban", "Unban", "UpdateStatus", "Resolve"))
            return "approve";

        if (Contains(actionName, "Delete") || Contains(actionName, "Remove")) return "delete";
        if (Contains(actionName, "Create") || Contains(actionName, "Add")) return "create";
        if (Contains(actionName, "Update") || Contains(actionName, "Edit")) return "update";

        // Ad bir şey söylemiyorsa HTTP fiiline bak: okuma serbest, yazma "update" sayılır
        // (daha dar taraf — yanlış türetme yetki AÇMAZ, kapatır).
        return HttpMethods.IsGet(httpMethod) || HttpMethods.IsHead(httpMethod) ? "read" : "update";
    }

    private static bool StartsWithAny(string name, params string[] prefixes)
        => prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static bool Contains(string name, string fragment)
        => name.Contains(fragment, StringComparison.OrdinalIgnoreCase);

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Kimlik doğrulamasını [Authorize] hâlleder; buraya giriş yapmamış biri düşerse
        // yönlendirmeyi ona bırakırız (çift yanıt üretmemek için).
        if (user.Identity?.IsAuthenticated != true) return;

        var role = user.FindFirstValue(ClaimTypes.Role);
        if (role is "super_admin" or "admin") return; // matris yalnız moderatör içindir

        if (role != "moderator" || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var actionName = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName ?? string.Empty;
        var action = ActionFor(actionName, context.HttpContext.Request.Method);

        if (!await _permissions.HasAsync(userId, _module, action, context.HttpContext.RequestAborted))
            context.Result = new ForbidResult();
    }
}
