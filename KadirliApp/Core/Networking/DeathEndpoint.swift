import Foundation

enum DeathEndpoint: Endpoint {
    case getDeaths(date: String) // Format: YYYY-MM-DD
    
    var path: String {
        switch self {
        case .getDeaths(let date):
            // Supabase sorgusu: death_date sütunu seçilen tarihe eşit olanlar
            return "/deaths?death_date=eq.\(date)&order=first_name.asc"
        }
    }
    
    var method: HTTPMethod {
        return .GET
    }
}
