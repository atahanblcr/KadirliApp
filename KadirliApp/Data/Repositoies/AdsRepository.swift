import Foundation

protocol AdsRepositoryProtocol {
    func fetchAds() async throws -> [Ad]
    func deleteAd(id: String) async throws // Yeni fonksiyon
}

final class AdsRepository: AdsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchAds() async throws -> [Ad] {
        return try await networkManager.request(endpoint: AdsEndpoint.getActiveAds)
    }
    
    // YENİ EKLENEN FONKSİYON
    func deleteAd(id: String) async throws {
        // Dönüş tipini önemsemiyoruz (Void), hata atmazsa başarılıdır.
        let _: String? = try? await networkManager.request(endpoint: AdsEndpoint.softDeleteAd(id: id))
    }
}

