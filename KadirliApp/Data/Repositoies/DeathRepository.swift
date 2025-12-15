import Foundation

protocol DeathRepositoryProtocol {
    func fetchDeaths(for date: Date) async throws -> [DeathNotice]
}

final class DeathRepository: DeathRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    private let dateFormatter: DateFormatter
    
    init() {
        self.dateFormatter = DateFormatter()
        self.dateFormatter.dateFormat = "yyyy-MM-dd"
    }
    
    func fetchDeaths(for date: Date) async throws -> [DeathNotice] {
        let dateString = dateFormatter.string(from: date)
        return try await networkManager.request(endpoint: DeathEndpoint.getDeaths(date: dateString))
    }
}
