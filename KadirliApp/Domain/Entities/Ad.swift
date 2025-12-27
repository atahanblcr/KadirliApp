import Foundation
import SwiftUI

enum AdType: String, Codable, CaseIterable {
    // Çıkarılanlar: job (İş), animals (Hayvanlar)
    // Kalanlar ve Yeni İsimler:
    case secondHand = "second_hand"
    case zeroProduct = "zero_product" // Yeni: Sıfır Ürün (Eski 'service' yerine veya ek olarak)
    case realEstate = "real_estate"
    case vehicle = "vehicle"
    case service = "service" // Hizmet (Boyacı, Nakliye vb.)
    case spareParts = "spare_parts"
    
    var displayName: String {
        switch self {
        case .secondHand: return "2. El Eşya"
        case .zeroProduct: return "Sıfır Ürün"
        case .realEstate: return "Emlak"
        case .vehicle: return "Vasıta"
        case .service: return "Hizmet & Kiralama"
        case .spareParts: return "Yedek Parça"
        }
    }
    
    var color: Color {
        switch self {
        case .secondHand: return .brown
        case .zeroProduct: return .blue
        case .realEstate: return .green
        case .vehicle: return .red
        case .service: return .orange
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
