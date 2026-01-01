import Foundation

protocol AnnouncementRepositoryProtocol {
    func fetchAnnouncements() async throws -> [Announcement]
}

final class AnnouncementRepository: AnnouncementRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchAnnouncements() async throws -> [Announcement] {
        return try await networkManager.request(endpoint: AnnouncementEndpoint.getAnnouncements)
    }
}
