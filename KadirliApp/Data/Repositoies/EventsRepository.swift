import Foundation

protocol EventsRepositoryProtocol {
    func fetchEvents() async throws -> [Event]
}

final class EventsRepository: EventsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchEvents() async throws -> [Event] {
        return try await networkManager.request(endpoint: EventsEndpoint.getActiveEvents)
    }
}
