import Foundation

enum PharmacyEndpoint: Endpoint {
    case getDutyPharmacies(date: String) // Format: YYYY-MM-DD
    
    var path: String {
        switch self {
        case .getDutyPharmacies(let date):
            // 'duty_date' bugüne eşit veya büyük olanları getir.
            // Bölgeye göre sırala (Lefkoşa, Girne...)
            return "/pharmacies?duty_date=gte.\(date)&order=region.asc"
        }
    }
    
    var method: HTTPMethod {
        return .GET
    }
}
