import Foundation

/// API isteklerini yapılandırmak için kullanılan protokol.
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
    // Varsayılan değerler (Burayı güncelledik)
    var headers: [String: String]? {
        return [
            "Content-Type": "application/json",
            "ApiKey": AppConfig.supabaseKey, // Config dosyasından okuyor
            "Authorization": "Bearer \(AppConfig.supabaseKey)" // Supabase için gerekli
        ]
    }
    
    // İŞTE EKSİK OLAN KISIM BURASIYDI:
    // Çoğu istekte body (veri gövdesi) boş olacağı için varsayılan olarak nil döndürüyoruz.
    var body: Data? {
        return nil
    }
}
