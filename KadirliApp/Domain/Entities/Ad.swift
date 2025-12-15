import Foundation
import SwiftUI

enum AdType: String, Codable, CaseIterable {
    case job, service, tender, realEstate = "real_estate"
    case vehicle, secondHand = "second_hand", animals, spareParts = "spare_parts"
    
    var displayName: String {
        switch self {
        case .job: return "İş İlanı"
        case .service: return "Hizmet"
        case .tender: return "İhale"
        case .realEstate: return "Emlak"
        case .vehicle: return "Vasıta"
        case .secondHand: return "İkinci El"
        case .animals: return "Hayvanlar"
        case .spareParts: return "Yedek Parça"
        }
    }
    
    var color: Color {
        switch self {
        case .job: return .blue
        case .service: return .orange
        case .tender: return .purple
        case .realEstate: return .green
        case .vehicle: return .red
        case .secondHand: return .brown
        case .animals: return .pink
        case .spareParts: return .gray
        }
    }
}

struct Ad: Identifiable, Codable {
    let id: UUID
    let title: String
    let description: String?
    let type: AdType
    let contactInfo: String?
    let price: String?
    let imageUrls: [String]?
    let expiresAt: Date?
    let isActive: Bool
    
    enum CodingKeys: String, CodingKey {
        case id, title, description, type, price
        case contactInfo = "contact_info"
        case imageUrls = "image_urls"
        case expiresAt = "expires_at"
        case isActive = "is_active"
    }
}
