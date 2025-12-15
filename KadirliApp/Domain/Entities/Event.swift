import Foundation

struct Event: Identifiable, Codable {
    let id: UUID
    let title: String
    let description: String?
    let eventDate: Date
    let locationName: String?
    let latitude: Double?
    let longitude: Double?
    let imageUrl: String?
    let isActive: Bool
    
    enum CodingKeys: String, CodingKey {
        case id, title, description
        case eventDate = "event_date"
        case locationName = "location_name"
        case latitude, longitude
        case imageUrl = "image_url"
        case isActive = "is_active"
    }
}
