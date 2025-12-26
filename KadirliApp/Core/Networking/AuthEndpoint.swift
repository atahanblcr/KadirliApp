import Foundation

enum AuthEndpoint: Endpoint {
    // Eski login/register yerine artık bunlar var:
    case sendOTP(phone: String)
    case verifyOTP(phone: String, token: String)
    case updateProfile(userId: String, fullName: String, neighborhood: String)
    
    var path: String {
        switch self {
        case .sendOTP:
            return "/auth/v1/otp"
        case .verifyOTP:
            return "/auth/v1/verify"
        case .updateProfile(let userId, _, _):
            return "/profiles?id=eq.\(userId)"
        }
    }
    
    var method: HTTPMethod {
        switch self {
        case .sendOTP, .verifyOTP:
            return .POST
        case .updateProfile:
            return .PATCH
        }
    }
    
    var body: Data? {
        switch self {
        case .sendOTP(let phone):
            let params = ["phone": phone]
            return try? JSONSerialization.data(withJSONObject: params)
            
        case .verifyOTP(let phone, let token):
            let params = [
                "type": "sms",
                "phone": phone,
                "token": token
            ]
            return try? JSONSerialization.data(withJSONObject: params)
            
        case .updateProfile(_, let fullName, let neighborhood):
            let params = [
                "full_name": fullName,
                "neighborhood": neighborhood
            ]
            return try? JSONSerialization.data(withJSONObject: params)
        }
    }
}
