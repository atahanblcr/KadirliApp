import Foundation

struct Place: Identifiable, Codable {
    let id: UUID
    let title: String
    let description: String?
    let distanceText: String? // "Merkeze 15 km" yazısı
    let distanceKm: Double?   // 15.0 (Sıralama için sayı)
    let latitude: Double?
    let longitude: Double?
    let imageUrls: [String]?
    let createdAt: Date
    
    enum CodingKeys: String, CodingKey {
        case id, title, description, latitude, longitude
        case distanceText = "distance_text"
        case distanceKm = "distance_km" // Yeni alan
        case imageUrls = "image_urls"
        case createdAt = "created_at"
    }
}
