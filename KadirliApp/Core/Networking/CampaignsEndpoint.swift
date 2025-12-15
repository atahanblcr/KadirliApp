import Foundation

enum CampaignsEndpoint: Endpoint {
    case getCampaigns
    
    var path: String {
        switch self {
        case .getCampaigns:
            return "/campaigns?order=created_at.desc"
        }
    }
    
    var method: HTTPMethod { .GET }
}
