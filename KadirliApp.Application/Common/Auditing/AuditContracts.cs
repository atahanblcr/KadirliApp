using System;
using System.Net;

namespace KadirliApp.Application.Common.Auditing;

/// <summary>
/// Faz 10.9(i): Bu arayüzü uygulayan command'ler başarıyla tamamlandığında
/// AuditBehavior audit_logs tablosuna iz yazar (merkezî çözüm — handler'lara kod girmez).
/// KAPSAM KARARI: onay/ret, ban, silme, personel ve doğrulama gibi hassas admin aksiyonları;
/// salt içerik düzenleme (Update) komutları bilinçli kapsam dışı (gürültü olur).
/// </summary>
public interface IAuditableCommand
{
    /// <summary>Modül adı — RequirePermission modül string'leriyle aynı sözlük ("ads", "users", "staff"...).</summary>
    string AuditModule { get; }

    /// <summary>Eylem ("approve", "reject", "ban", "delete", "create"...).</summary>
    string AuditAction { get; }

    /// <summary>Etkilenen kayıt — create komutlarında null bırakılır, davranış Guid dönen yanıttan alır.</summary>
    Guid? AuditAffectedId => null;

    /// <summary>Etkilenen entity adı (nameof(Ad) gibi).</summary>
    string? AuditAffectedType => null;

    /// <summary>jsonb details kolonuna yazılacak ek bilgi. ŞİFRE GİBİ SIRLAR ASLA KONMAZ — komutun tamamı bilinçli serialize edilmiyor.</summary>
    object? AuditDetails => null;
}

/// <summary>
/// İsteği yapan aktörün kimliği — host (Api: user_id claim'i, Web: NameIdentifier) sağlar.
/// HTTP dışı bağlamda (Hangfire job) UserId null'dır ve audit yazılmaz.
/// </summary>
public interface IAuditContext
{
    Guid? UserId { get; }
    IPAddress? IpAddress { get; }
    string? UserAgent { get; }
}
