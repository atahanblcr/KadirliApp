import Foundation

enum PlacesEndpoint: Endpoint {
    case getPlaces
    
    var path: String {
        switch self {
        case .getPlaces: return "/places?order=title.asc"
        }
    }
    
    var method: HTTPMethod { .GET }
    
    var body: Data? { nil } // GET isteği olduğu için body yok
}
