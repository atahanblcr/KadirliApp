import Foundation

protocol PlaceRepositoryProtocol {
    func fetchPlaces() async throws -> [Place]
}

final class PlaceRepository: PlaceRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchPlaces() async throws -> [Place] {
        return try await networkManager.request(endpoint: PlacesEndpoint.getPlaces)
    }
}
