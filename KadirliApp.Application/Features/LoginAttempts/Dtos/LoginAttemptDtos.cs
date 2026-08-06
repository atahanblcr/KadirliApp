namespace KadirliApp.Application.Features.LoginAttempts.Dtos;

/// <summary>
/// Faz 12.2 — panelin gördüğü giriş denemesi.
///
/// ⚠️ <c>Identifier</c> zaten <b>maskeli</b> olarak saklanıyor; bu DTO'nun ham hâline
/// erişimi yok ve olmayacak. "Panelde ham göstersek de olur" düşüncesi maskelemeyi
/// anlamsızlaştırırdı: kayıtlar buradan <b>CSV'ye</b> de çıkıyor.
/// </summary>
public class LoginAttemptResponseDto
{
    public Guid Id { get; set; }
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuspicious { get; set; }
    public string? SuspicionRule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AlertedAt { get; set; }
}

public class QueryLoginAttemptDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 25;

    public string? Channel { get; set; }

    /// <summary>null → tümü, true → yalnız başarılı, false → yalnız başarısız.</summary>
    public bool? Succeeded { get; set; }

    /// <summary>true → yalnız şüpheli işaretliler. null/false → süzme yok.</summary>
    public bool? Suspicious { get; set; }

    public string? Rule { get; set; }
    public string? Reason { get; set; }

    /// <summary>Tek bir hesabın geçmişi (kullanıcı/personel detay kutusu).</summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// <see cref="UserId"/> ile <b>VEYA</b> ilişkisiyle birleşir: hatalı OTP kayıtlarında
    /// <c>UserId</c> boştur (hesap tablosuna dokunulmaz) ve o satırlar yalnız maskeli
    /// kimlikle hesaba bağlanabilir. İkisinden biri kullanılırsa kullanıcı ekranı
    /// başarısız denemeleri <b>hiç göstermez</b>.
    /// </summary>
    public string? MaskedIdentifier { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Maskeli kimlik içinde arar (<c>+90500***</c> yazmak yeter).</summary>
    public string? Search { get; set; }

    /// <summary>
    /// IP <b>tam eşleşme</b> ile süzülür, "içerir" ile değil.
    /// </summary>
    /// <remarks>
    /// ⚠️ İki sebep: (1) <c>inet</c> kolonunda <c>ToString().Contains()</c> SQL'e
    /// çevrilemez — Npgsql patlar ya da sorguyu istemciye çeker (tam tarama);
    /// (2) "10.0.0.1 içerir" araması 110.0.0.15'i de getirir, yani güvenlik ekranında
    /// <b>yanlış</b> cevap verir. Geçersiz IP metni süzgeci sessizce yok saymaz —
    /// sonuç boş döner ve panel bunu söyler.
    /// </remarks>
    public string? Ip { get; set; }

    public string? Sort { get; set; }
}
