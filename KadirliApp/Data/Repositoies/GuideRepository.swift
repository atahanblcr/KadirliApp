import Foundation

// VERİ MODELİ (Bunu buraya koyduk ki hem Repo hem View kullanabilsin)
struct TaxiRequest: Identifiable, Decodable {
    let id: UUID
    let passengerPhone: String?
    let pickupLatitude: Double?
    let pickupLongitude: Double?
    let status: String?
    let createdAt: String? // Supabase'den tarih string gelir
    
    var formattedTime: String {
        // Basit tarih gösterimi (İsteğe göre geliştirilebilir)
        return createdAt ?? "Az önce"
    }
    
    enum CodingKeys: String, CodingKey {
        case id, status
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
    func fetchTaxiRequests(taxiId: String) async throws -> [TaxiRequest]
    func updateRequestStatus(requestId: String, status: String) async throws
    func updateTaxiBusyStatus(taxiId: String, isBusy: Bool) async throws
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
    
    func fetchTaxiRequests(taxiId: String) async throws -> [TaxiRequest] {
        return try await networkManager.request(endpoint: GuideEndpoint.getTaxiRequests(taxiId: taxiId))
    }
    
    func updateRequestStatus(requestId: String, status: String) async throws {
        let _: String? = try? await networkManager.request(
            endpoint: GuideEndpoint.updateTaxiRequestStatus(requestId: requestId, status: status)
        )
    }
    
    func updateTaxiBusyStatus(taxiId: String, isBusy: Bool) async throws {
            let params = ["is_busy": isBusy]
            let data = try JSONSerialization.data(withJSONObject: params)
            
            // URL'in doğru olduğundan emin olalım (Sondaki slash vb. sorun olmasın)
            let cleanUrl = AppConfig.supabaseUrl.replacingOccurrences(of: "/rest/v1", with: "")
            let urlString = "\(cleanUrl)/rest/v1/guide_items?id=eq.\(taxiId)"
            
            guard let url = URL(string: urlString) else { return }
            
            var request = URLRequest(url: url)
            request.httpMethod = "PATCH"
            request.allHTTPHeaderFields = [
                "ApiKey": AppConfig.supabaseKey,
                "Authorization": "Bearer \(AppConfig.supabaseKey)",
                "Content-Type": "application/json",
                "Prefer": "return=minimal" // Supabase'e "Bana cevap gövdesi yollama" der
            ]
            
            // Token varsa ekle
            if let tokenData = KeychainHelper.standard.read(service: "com.atahanblcr.KadirliApp.token", account: "auth_token"),
               let token = String(data: tokenData, encoding: .utf8) {
                request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
            }
            
            request.httpBody = data
            
            print("🌍 Durum Güncelleniyor: \(url.absoluteString) -> \(isBusy)")
            
            let (responseData, response) = try await URLSession.shared.data(for: request)
            
            if let httpResponse = response as? HTTPURLResponse {
                if !(200...299).contains(httpResponse.statusCode) {
                    // HATA DETAYINI YAZDIR
                    if let errorText = String(data: responseData, encoding: .utf8) {
                        print("❌ HATA DETAYI: \(errorText)")
                    }
                    throw AppError.serverError(statusCode: httpResponse.statusCode)
                }
            }
        }
    }

