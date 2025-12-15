import Foundation

/// API isteklerini yapılandırmak için kullanılan protokol.
/// Her yeni API isteği bu protokolü uygulayan bir Enum case'i olmalıdır.
protocol Endpoint {
    var path: String { get }
    var method: HTTPMethod { get }
    var headers: [String: String]? { get }
    var body: Data? { get }
}

enum HTTPMethod: String {
    case GET
    case POST
    case PUT
    case DELETE
    case PATCH
}

extension Endpoint {
    // Varsayılan değerler
    var headers: [String: String]? {
        return ["Content-Type": "application/json", "ApiKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImR0ZmpnYmplZ2twaGxncXpscGx3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjUxMzk1NjQsImV4cCI6MjA4MDcxNTU2NH0.xF750JrWbg0ktUSbgbltw4sMoDtLXw5Xy2EfOyH00Mw"]
        // Not: Gerçek projede Key'ler AppConfig'den gelmelidir.
    }
    
    var body: Data? {
        return nil
    }
}


