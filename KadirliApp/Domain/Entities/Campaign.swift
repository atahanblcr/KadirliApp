import Foundation

struct Campaign: Identifiable, Codable {
    let id: UUID
    let title: String
    let businessName: String
    let description: String?
    let discountCode: String?
    let imageUrls: [String]?
    let createdAt: Date
    
    enum CodingKeys: String, CodingKey {
        case id, title, description
        case businessName = "business_name"
        case discountCode = "discount_code"
        case imageUrls = "image_urls"
        case createdAt = "created_at"
    }
}
