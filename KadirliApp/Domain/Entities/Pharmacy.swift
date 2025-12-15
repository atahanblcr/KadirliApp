import Foundation

struct Pharmacy: Identifiable, Codable {
    let id: UUID
    let name: String
    let phone: String?
    let address: String?
    let region: String
    let latitude: Double?
    let longitude: Double?
    let dutyDate: String // YYYY-MM-DD formatında String
    
    enum CodingKeys: String, CodingKey {
        case id, name, phone, address, region, latitude, longitude
        case dutyDate = "duty_date"
    }
}
