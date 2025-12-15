import Foundation

/// Kullanıcının bildirim tercihlerini tutan model.
/// Supabase'deki JSONB yapısına karşılık gelir.
struct NotificationPreferences: Codable, Equatable {
    var news: Bool
    var deaths: Bool
    var pharmacy: Bool
    var events: Bool
    
    // Varsayılan değerler
    static let `default` = NotificationPreferences(
        news: true,
        deaths: true,
        pharmacy: false,
        events: true
    )
}

