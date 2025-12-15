import Foundation

enum EventsEndpoint: Endpoint {
    case getActiveEvents
    
    var path: String {
        switch self {
        case .getActiveEvents:
            // Bugünden itibaren olan aktif etkinlikleri getir, tarihe göre sırala
            let today = ISO8601DateFormatter().string(from: Date())
            return "/events?is_active=eq.true&event_date=gte.\(today)&order=event_date.asc"
        }
    }
    
    var method: HTTPMethod { return .GET }
}

