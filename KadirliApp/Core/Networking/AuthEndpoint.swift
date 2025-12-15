import Foundation

enum AuthEndpoint: Endpoint {
    case login(email: String, pass: String)
    case register(email: String, pass: String, fullName: String)
    
    var path: String {
        switch self {
        case .login:
            return "/auth/v1/token?grant_type=password"
        case .register:
            return "/auth/v1/signup"
        }
    }
    
    var method: HTTPMethod {
        return .POST
    }
    
    var body: Data? {
        switch self {
        case .login(let email, let pass):
            let params = ["email": email, "password": pass]
            return try? JSONSerialization.data(withJSONObject: params)
            
        case .register(let email, let pass, let fullName):
            let data = [
                "email": email,
                "password": pass,
                "data": ["full_name": fullName]
            ] as [String : Any]
            return try? JSONSerialization.data(withJSONObject: data)
        }
    }
}
