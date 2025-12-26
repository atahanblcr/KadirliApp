import Foundation

/// Uygulamanın ağ trafiğini yöneten Singleton sınıf.
/// Generic yapısı sayesinde her türlü Decodable veriyi işleyebilir.
final class NetworkManager {
    
    static let shared = NetworkManager()
    
    private let session: URLSession
    private let decoder: JSONDecoder
    
    // ⚠️ DİKKAT: Buraya kendi Supabase proje URL'ini yapıştırdığından emin ol!
    let baseURL = "https://dtfjgbjegkphlgqzlplw.supabase.co/rest/v1"
    
    private init() {
        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 30
        self.session = URLSession(configuration: config)
        
        self.decoder = JSONDecoder()
        // Supabase tarih formatı (ISO8601) için strateji
        self.decoder.dateDecodingStrategy = .iso8601
    }
    
    /// Generic API İstek Fonksiyonu
    func request<T: Decodable>(endpoint: Endpoint) async throws -> T {
        
        // 1. URL Hazırlığı
        // Eğer istek Authentication (Giriş/Kayıt) ile ilgiliyse URL'den "/rest/v1" kısmını çıkarıyoruz.
        var effectiveBaseURL = baseURL
        if endpoint.path.hasPrefix("/auth") {
            effectiveBaseURL = baseURL.replacingOccurrences(of: "/rest/v1", with: "")
        }
        
        guard let url = URL(string: effectiveBaseURL + endpoint.path) else {
            throw AppError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = endpoint.method.rawValue
        
        // 2. HEADER AYARLAMALARI (İŞTE EKSİK OLAN KISIM BURASIYDI 🛠️)
        var headers = endpoint.headers ?? [:]
        
        // Eğer Keychain'de kayıtlı bir kullanıcı Token'ı varsa,
        // "Authorization" başlığını bu Token ile değiştir.
        // Böylece sunucu "Heh, bu işlemi yapan Ahmet'miş" diyebilecek.
        if let data = KeychainHelper.standard.read(service: "com.atahanblcr.KadirliApp.token", account: "auth_token"),
           let token = String(data: data, encoding: .utf8), !token.isEmpty {
            headers["Authorization"] = "Bearer \(token)"
            print("🔑 İstek Kullanıcı Token'ı ile imzalandı.")
        }
        
        request.allHTTPHeaderFields = headers
        request.httpBody = endpoint.body
        
        // Debug için yazdır
        print("🌍 İstek Yapılıyor: \(url.absoluteString)")
        
        do {
            let (data, response) = try await session.data(for: request)
            
            guard let httpResponse = response as? HTTPURLResponse else {
                throw AppError.serverError(statusCode: 0)
            }
            
            // Başarılı durum kodları (200-299)
            guard (200...299).contains(httpResponse.statusCode) else {
                if let errorString = String(data: data, encoding: .utf8) {
                    print("❌ Sunucu Hatası: \(errorString)")
                }
                
                if httpResponse.statusCode == 401 {
                    throw AppError.unauthorized
                }
                throw AppError.serverError(statusCode: httpResponse.statusCode)
            }
            
            // ✅ DÜZELTME: Eğer veri boşsa ama işlem başarılıysa (Örn: 204 No Content)
            if data.isEmpty {
                if (200...299).contains(httpResponse.statusCode) {
                    // JSONDecoder'a "null" veriyoruz.
                    // Bu sayede String? veya UserDTO? gibi Optional tipler otomatik olarak 'nil' olur ve hata vermez.
                    let emptyData = "null".data(using: .utf8)!
                    return try decoder.decode(T.self, from: emptyData)
                }
                throw AppError.noData
            }
            
            // Decoding işlemi
            do {
                let decodedData = try decoder.decode(T.self, from: data)
                return decodedData
            } catch let decodingError as DecodingError {
                print("⚠️ Decoding Hatası: \(decodingError)")
                throw AppError.decodingError(decodingError.localizedDescription)
            }
            
        } catch let error as AppError {
            throw error
        } catch {
            throw AppError.unknown(error)
        }
    }
}
