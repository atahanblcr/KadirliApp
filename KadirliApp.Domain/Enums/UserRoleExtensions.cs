namespace KadirliApp.Domain.Enums;

public static class UserRoleExtensions
{
    /// <summary>
    /// Rol claim'i / policy kontrolü için kullanılan snake_case rol adı
    /// (Program.cs'teki "admin", "super_admin", "moderator" policy'leriyle birebir uyumlu).
    /// </summary>
    public static string ToRoleString(this UserRole role) => role switch
    {
        UserRole.SuperAdmin => "super_admin",
        UserRole.Admin => "admin",
        UserRole.Moderator => "moderator",
        _ => "user"
    };
}
