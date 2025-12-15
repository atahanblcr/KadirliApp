import Foundation

enum AdsEndpoint: Endpoint {
    case getActiveAds
    
    var path: String {
        switch self {
        case .getActiveAds:
            return "/ads?is_active=eq.true&order=created_at.desc"
        }
    }
    
    var method: HTTPMethod { return .GET }
}

