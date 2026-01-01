import Foundation
import SwiftUI

enum AnnouncementType: String, Codable, CaseIterable {
    case electricity = "electricity" // Elektrik Kesintisi
    case water = "water"             // Su Kesintisi
    case institution = "institution" // Resmi Kurum
    case education = "education"     // Eğitim/Sınav
    case general = "general"         // Genel Duyuru
    
    var title: String {
        switch self {
        case .electricity: return "Elektrik Kesintisi"
        case .water: return "Su Kesintisi"
        case .institution: return "Kurum Duyurusu"
        case .education: return "Eğitim & Sınav"
        case .general: return "Genel Duyuru"
        }
    }
    
    var iconName: String {
        switch self {
        case .electricity: return "bolt.fill"
        case .water: return "drop.fill"
        case .institution: return "building.columns.fill"
        case .education: return "book.fill"
        case .general: return "megaphone.fill"
        }
    }
    
    var color: Color {
        switch self {
        case .electricity: return .yellow
        case .water: return .blue
        case .institution: return .red
        case .education: return .orange
        case .general: return .gray
        }
    }
}

struct Announcement: Identifiable, Codable {
    let id: UUID
    let title: String
    let description: String
    let type: AnnouncementType
    let institutionName: String?
    let imageUrl: String?
    let fileUrl: String?
    let targetNeighborhoods: [String]? // Etkilenen mahalleler
    let createdAt: Date
    
    enum CodingKeys: String, CodingKey {
        case id, title, description, type
        case institutionName = "institution_name"
        case imageUrl = "image_url"
        case fileUrl = "file_url"
        case targetNeighborhoods = "target_neighborhoods"
        case createdAt = "created_at"
    }
}
