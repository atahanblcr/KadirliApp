import Foundation

// VERİ MODELİ (Bunu buraya koyduk ki hem Repo hem View kullanabilsin)
struct TaxiRequest: Identifiable, Decodable {
    let id: UUID
    let passengerPhone: String?
    let pickupLatitude: Double?
    let pickupLongitude: Double?
    let createdAt: String? // Supabase'den tarih string gelir
    
    var formattedTime: String {
        // Basit tarih gösterimi (İsteğe göre geliştirilebilir)
        return createdAt ?? "Az önce"
    }
    
    enum CodingKeys: String, CodingKey {
        case id
        case passengerPhone = "user_phone" // SQL: user_phone
        case pickupLatitude = "latitude"   // SQL: latitude
        case pickupLongitude = "longitude" // SQL: longitude
        case createdAt = "created_at"
    }
}

protocol GuideRepositoryProtocol {
    func fetchCategories() async throws -> [GuideCategory]
    func fetchItems(categoryId: String) async throws -> [GuideItem]
    func getDriverTaxiId(userId: String) async -> String?
    func sendTaxiRequest(taxiId: String, phone: String, lat: Double, long: Double) async throws
    // YENİ: İstekleri Getir
    func fetchTaxiRequests(taxiId: String) async throws -> [TaxiRequest]
}

final class GuideRepository: GuideRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchCategories() async throws -> [GuideCategory] {
        return try await networkManager.request(endpoint: GuideEndpoint.getCategories)
    }
    
    func fetchItems(categoryId: String) async throws -> [GuideItem] {
        return try await networkManager.request(endpoint: GuideEndpoint.getItems(categoryId: categoryId))
    }
    
    func getDriverTaxiId(userId: String) async -> String? {
        struct TaxiIDResponse: Decodable { let id: UUID }
        do {
            let result: [TaxiIDResponse] = try await networkManager.request(endpoint: GuideEndpoint.getDriverTaxi(userId: userId))
            return result.first?.id.uuidString
        } catch {
            return nil
        }
    }
    
    func sendTaxiRequest(taxiId: String, phone: String, lat: Double, long: Double) async throws {
        let _: String? = try? await networkManager.request(
            endpoint: GuideEndpoint.requestTaxi(taxiId: taxiId, phone: phone, lat: lat, long: long)
        )
    }
    
    // YENİ: Gerçek veriyi çeken fonksiyon
    func fetchTaxiRequests(taxiId: String) async throws -> [TaxiRequest] {
        return try await networkManager.request(endpoint: GuideEndpoint.getTaxiRequests(taxiId: taxiId))
    }
}
