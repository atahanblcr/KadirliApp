import Foundation

enum TransportEndpoint: Endpoint {
    case getRoutes
    case getStopsForRoute(routeId: String)
    case getIntercityTrips
    
    var path: String {
        switch self {
        case .getRoutes:
            return "/transport_routes"
        case .getStopsForRoute(let routeId):
            // İlişkili tablodan (route_stops) durak bilgisini (stops) çekiyoruz (JOIN işlemi)
            return "/transport_route_stops?route_id=eq.\(routeId)&select=*,stop:transport_stops(*)&order=stop_order.asc"
        case .getIntercityTrips:
            return "/intercity_trips?order=destination.asc"
        }
    }
    
    var method: HTTPMethod { .GET }
}
