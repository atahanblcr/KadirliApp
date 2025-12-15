import Foundation

protocol AdsRepositoryProtocol {
    func fetchAds() async throws -> [Ad]
}

final class AdsRepository: AdsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchAds() async throws -> [Ad] {
        return try await networkManager.request(endpoint: AdsEndpoint.getActiveAds)
    }
}

