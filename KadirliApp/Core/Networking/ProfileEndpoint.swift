import Foundation

enum ProfileEndpoint: Endpoint {
    case getProfile(userId: String)
    case updatePreferences(userId: String, prefs: NotificationPreferences)
    
    var path: String {
        switch self {
        case .getProfile(let userId):
            return "/profiles?id=eq.\(userId)"
        case .updatePreferences(let userId, _):
            return "/profiles?id=eq.\(userId)"
        }
    }
    
    var method: HTTPMethod {
        switch self {
        case .getProfile: return .GET
        case .updatePreferences: return .PATCH
        }
    }
    
    var body: Data? {
        switch self {
        case .getProfile:
            return nil
        case .updatePreferences(_, let prefs):
            // Sadece notification_preferences alanını güncelle
            let body = ["notification_preferences": prefs]
            return try? JSONEncoder().encode(body)
        }
    }
}
