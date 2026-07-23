using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Application.Features.Staff.Commands;

/// <summary>
/// Kontrattaki snake_case rol adını UserRole'a çevirir. Staff yalnızca moderator/admin
/// olabilir — super_admin API'den atanamaz (tek super_admin seed'den gelir).
/// </summary>
public static class StaffRole
{
    public static UserRole Parse(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "moderator" => UserRole.Moderator,
        "admin" => UserRole.Admin,
        _ => throw new AppException(
            "Geçersiz personel rolü. 'moderator' veya 'admin' olmalı.", "INVALID_ROLE")
    };
}
