import Foundation

enum GuideEndpoint: Endpoint {
    case getCategories
    case getItems(categoryId: String)
    case getDriverTaxi(userId: String)
    case requestTaxi(taxiId: String, phone: String, lat: Double, long: Double)
    case getTaxiRequests(taxiId: String)
    

    case updateTaxiRequestStatus(requestId: String, status: String)
    
    var path: String {
        switch self {
        case .getCategories:
            return "/guide_categories?order=rank.asc"
        case .getItems(let categoryId):
            return "/guide_items?category_id=eq.\(categoryId)&order=rank.asc"
        case .getDriverTaxi(let userId):
            return "/guide_items?owner_id=eq.\(userId)&select=id"
        case .requestTaxi:
            return "/taxi_requests"
        case .getTaxiRequests(let taxiId):
            // Sadece bekleyenleri değil, kabul edilenleri de çekelim ki şoför işlem yapabilsin
            return "/taxi_requests?taxi_id=eq.\(taxiId)&status=in.(pending,accepted)&order=created_at.desc"
            
        
        case .updateTaxiRequestStatus(let requestId, _):
            return "/taxi_requests?id=eq.\(requestId)"
        }
    }
    
    var method: HTTPMethod {
        switch self {
        case .requestTaxi: return .POST
        // 👇 YENİ: Güncelleme için PATCH kullanılır
        case .updateTaxiRequestStatus: return .PATCH
        default: return .GET
        }
    }
    
    var body: Data? {
        switch self {
        case .requestTaxi(let taxiId, let phone, let lat, let long):
            let params: [String: Any] = [
                "taxi_id": taxiId,
                "user_phone": phone,
                "latitude": lat,
                "longitude": long,
                "status": "pending"
            ]
            return try? JSONSerialization.data(withJSONObject: params)
            
        // 👇 YENİ: Sadece status bilgisini gönderiyoruz
        case .updateTaxiRequestStatus(_, let status):
            let params = ["status": status]
            return try? JSONSerialization.data(withJSONObject: params)
            
        default: return nil
        }
    }
}
