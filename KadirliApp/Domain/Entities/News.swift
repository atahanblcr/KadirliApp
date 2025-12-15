import Foundation

struct News: Identifiable, Codable {
    let id: UUID
    let title: String
    let content: String
    let summary: String?
    let imageUrls: [String]?
    let source: String?
    let category: String
    let isPublished: Bool
    let viewCount: Int
    let publishedAt: Date?
    let createdAt: Date
    
    // SQL (snake_case) -> Swift (camelCase) dönüşümü
    enum CodingKeys: String, CodingKey {
        case id, title, content, summary, source, category
        case imageUrls = "image_urls"
        case isPublished = "is_published"
        case viewCount = "view_count"
        case publishedAt = "published_at"
        case createdAt = "created_at"
    }
}
