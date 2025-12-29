import Foundation

enum StorageEndpoint: Endpoint {
    case uploadImage(data: Data, fileName: String)
    
    var path: String {
        switch self {
        case .uploadImage(_, let fileName):
            // 'ads' bucket'ı içine yüklüyoruz.
            // Supabase Storage API yapısı: /storage/v1/object/{bucket}/{filename}
            return "/storage/v1/object/ads/\(fileName)"
        }
    }
    
    var method: HTTPMethod { .POST }
    
    var headers: [String : String]? {
        // Standart headerlara ek olarak resim olduğunu belirtiyoruz
        let defaultHeaders = [
            "ApiKey": AppConfig.supabaseKey,
            "Authorization": "Bearer \(AppConfig.supabaseKey)",
            "Content-Type": "image/jpeg" // Sadece JPEG gönderiyoruz
        ]
        // Upsert (varsa üzerine yaz) kapalı olsun istiyorsak:
        // defaultHeaders["x-upsert"] = "false"
        return defaultHeaders
    }
    
    var body: Data? {
        switch self {
        case .uploadImage(let data, _):
            return data
        }
    }
}

