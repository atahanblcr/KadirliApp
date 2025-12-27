import Foundation

struct GuideCategory: Identifiable, Codable {
    let id: UUID
    let title: String
    let iconName: String? // SF Symbol ismi (Örn: "person.2.fill")
    let rank: Int
    
    enum CodingKeys: String, CodingKey {
        case id, title, rank
        case iconName = "icon_name"
    }
}

