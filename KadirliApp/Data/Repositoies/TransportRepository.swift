import Foundation

protocol TransportRepositoryProtocol {
    func fetchRoutes() async throws -> [TransportRoute]
    func fetchStops(for routeId: String) async throws -> [RouteStopItem]
    func fetchIntercityTrips() async throws -> [IntercityTrip]
}

final class TransportRepository: TransportRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchRoutes() async throws -> [TransportRoute] {
        return try await networkManager.request(endpoint: TransportEndpoint.getRoutes)
    }
    
    func fetchStops(for routeId: String) async throws -> [RouteStopItem] {
        return try await networkManager.request(endpoint: TransportEndpoint.getStopsForRoute(routeId: routeId))
    }
    
    func fetchIntercityTrips() async throws -> [IntercityTrip] {
        return try await networkManager.request(endpoint: TransportEndpoint.getIntercityTrips)
    }
}

