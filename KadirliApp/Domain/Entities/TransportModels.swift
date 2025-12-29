import Foundation
import CoreLocation

// 1. Hat Modeli
struct TransportRoute: Identifiable, Decodable {
    let id: UUID
    let title: String
    let startTime: String // "07:00:00"
    let endTime: String   // "23:00:00"
    let frequencyMin: Int
    
    enum CodingKeys: String, CodingKey {
        case id, title
        case startTime = "start_time"
        case endTime = "end_time"
        case frequencyMin = "frequency_min"
    }
}

// 2. Durak Modeli (SQL Join cevabı için iç içe yapı)
struct RouteStopItem: Identifiable, Decodable {
    let id: UUID
    let minutesFromStart: Int
    let stop: TransportStop
    
    enum CodingKeys: String, CodingKey {
        case id
        case minutesFromStart = "minutes_from_start"
        case stop
    }
}

struct TransportStop: Identifiable, Decodable {
    let id: UUID
    let name: String
    let latitude: Double
    let longitude: Double
    
    var coordinate: CLLocationCoordinate2D {
        CLLocationCoordinate2D(latitude: latitude, longitude: longitude)
    }
}

// 3. Şehir Dışı Modeli
struct IntercityTrip: Identifiable, Decodable {
    let id: UUID
    let destination: String
    let companyName: String?
    let departureTimes: [String]
    let price: String?
    
    enum CodingKeys: String, CodingKey {
        case id, destination, price
        case companyName = "company_name"
        case departureTimes = "departure_times"
    }
}
