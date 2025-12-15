import Foundation

struct DeathNotice: Identifiable, Codable {
    let id: UUID
    let firstName: String
    let lastName: String
    let deathDate: String // Supabase DATE tipi bazen String gelebilir, duruma göre Date yapılabilir.
    let burialPlace: String?
    let burialTime: String? // TIME tipi String olarak gelir (HH:mm:ss)
    let condolenceAddress: String?
    let latitude: Double?
    let longitude: Double?
    let imageUrl: String?
    let createdAt: Date
    
    var fullName: String {
        return "\(firstName) \(lastName)"
    }
    
    enum CodingKeys: String, CodingKey {
        case id
        case firstName = "first_name"
        case lastName = "last_name"
        case deathDate = "death_date"
        case burialPlace = "burial_place"
        case burialTime = "burial_time"
        case condolenceAddress = "condolence_address"
        case latitude, longitude
        case imageUrl = "image_url"
        case createdAt = "created_at"
    }
}
