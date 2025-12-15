import Foundation

struct AuthResponseDTO: Decodable {
    let accessToken: String
    let user: UserDTO
    
    enum CodingKeys: String, CodingKey {
        case accessToken = "access_token"
        case user
    }
}

struct UserDTO: Decodable {
    let id: UUID
    let email: String?
    let appMetadata: [String: AnyCodable]?
    let userMetadata: [String: AnyCodable]?
    
    enum CodingKeys: String, CodingKey {
        case id, email
        case appMetadata = "app_metadata"
        case userMetadata = "user_metadata"
    }
}

// Helper for dynamic JSON decoding
struct AnyCodable: Decodable {
    var value: Any
    
    struct CodingKeys: CodingKey {
        var stringValue: String
        var intValue: Int?
        init?(stringValue: String) { self.stringValue = stringValue }
        init?(intValue: Int) { self.stringValue = "\(intValue)"; self.intValue = intValue }
    }
    
    init(from decoder: Decoder) throws {
        if let container = try? decoder.singleValueContainer() {
            if let intVal = try? container.decode(Int.self) { value = intVal; return }
            if let doubleVal = try? container.decode(Double.self) { value = doubleVal; return }
            if let stringVal = try? container.decode(String.self) { value = stringVal; return }
            if let boolVal = try? container.decode(Bool.self) { value = boolVal; return }
        }
        value = "unknown"
    }
}
