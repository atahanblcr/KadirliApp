import Foundation

struct GuideItem: Identifiable, Codable {
    let id: UUID
    let categoryId: UUID
    let title: String
    let phone: String?
    let address: String?
    let latitude: Double?
    let longitude: Double?
    let isCenter: Bool // Muhtarlar için: Merkez mi, Köy mü?
    let rank: Int
    
    enum CodingKeys: String, CodingKey {
        case id, title, phone, address, latitude, longitude, rank
        case categoryId = "category_id"
        case isCenter = "is_center"
    }
}
