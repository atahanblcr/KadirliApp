import Foundation

protocol NewsRepositoryProtocol {
    func fetchLatestNews() async throws -> [News]
}

final class NewsRepository: NewsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchLatestNews() async throws -> [News] {
        // NetworkManager generic yapısı sayesinde [News] tipini otomatik decode eder
        return try await networkManager.request(endpoint: NewsEndpoint.getLatestNews)
    }
}
