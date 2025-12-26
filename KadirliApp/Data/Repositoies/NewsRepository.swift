import Foundation

protocol NewsRepositoryProtocol {
    // Sayfa numarasını parametre olarak alıyoruz
    func fetchLatestNews(page: Int) async throws -> [News]
}

final class NewsRepository: NewsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    private let pageSize = 20 // Her seferde 20 haber çek
    
    func fetchLatestNews(page: Int) async throws -> [News] {
        // Sayfa 0 -> Offset 0
        // Sayfa 1 -> Offset 20
        // Sayfa 2 -> Offset 40 ...
        let offset = page * pageSize
        
        return try await networkManager.request(
            endpoint: NewsEndpoint.getLatestNews(limit: pageSize, offset: offset)
        )
    }
}
