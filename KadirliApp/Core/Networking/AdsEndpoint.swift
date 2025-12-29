import Foundation

enum AdsEndpoint: Endpoint {
    case getActiveAds
    case softDeleteAd(id: String)
    case createAd(adData: [String: Any]) // YENİ
    
    var path: String {
        switch self {
        case .getActiveAds:
            return "/ads?is_active=eq.true&is_deleted=eq.false&order=created_at.desc"
        case .softDeleteAd(let id):
            return "/ads?id=eq.\(id)"
        case .createAd: // YENİ
            return "/ads"
        }
    }
    
    var method: HTTPMethod {
        switch self {
        case .getActiveAds: return .GET
        case .softDeleteAd: return .PATCH
        case .createAd: return .POST // YENİ
        }
    }
    
    var body: Data? {
        switch self {
        case .getActiveAds:
            return nil
        case .softDeleteAd:
            let params = ["is_deleted": true]
            return try? JSONSerialization.data(withJSONObject: params)
        case .createAd(let adData): // YENİ
            return try? JSONSerialization.data(withJSONObject: adData)
        }
    }
}
