import Foundation

/// Uygulamanın ağ trafiğini yöneten Singleton sınıf.
/// Generic yapısı sayesinde her türlü Decodable veriyi işleyebilir.
final class NetworkManager {
    
    static let shared = NetworkManager()
    
    private let session: URLSession
    private let decoder: JSONDecoder
    
    // ⚠️ DİKKAT: Buraya kendi Supabase proje URL'ini yapıştırdığından emin ol!
    // Örnek: "https://abcdefghijklm.supabase.co/rest/v1"
    private let baseURL = "https://dtfjgbjegkphlgqzlplw.supabase.co/rest/v1"
    
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
        
        // 🛠️ DÜZELTME: Eğer istek Authentication (Giriş/Kayıt) ile ilgiliyse
        // URL'den "/rest/v1" kısmını çıkarıyoruz.
        var effectiveBaseURL = baseURL
        if endpoint.path.hasPrefix("/auth") {
            effectiveBaseURL = baseURL.replacingOccurrences(of: "/rest/v1", with: "")
        }
        
        guard let url = URL(string: effectiveBaseURL + endpoint.path) else {
            throw AppError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = endpoint.method.rawValue
        request.allHTTPHeaderFields = endpoint.headers
        request.httpBody = endpoint.body
        
        // Debug için yazdır (Hata alırsak konsoldan URL'i kontrol edebilirsin)
        print("🌍 İstek Yapılıyor: \(url.absoluteString)")
        
        do {
            let (data, response) = try await session.data(for: request)
            
            guard let httpResponse = response as? HTTPURLResponse else {
                throw AppError.serverError(statusCode: 0)
            }
            
            // Başarılı durum kodları (200-299)
            guard (200...299).contains(httpResponse.statusCode) else {
                // Supabase bazen hata detayını JSON döner, onu okuyabiliriz
                if let errorString = String(data: data, encoding: .utf8) {
                    print("❌ Sunucu Hatası: \(errorString)")
                }
                
                if httpResponse.statusCode == 401 {
                    throw AppError.unauthorized
                }
                throw AppError.serverError(statusCode: httpResponse.statusCode)
            }
            
            // Bazı Auth işlemleri (Örn: Update) boş veri dönebilir, hata vermesin
            if data.isEmpty {
                // Eğer T tipi Void veya benzeri bir şeyse handle edilebilir ama
                // şimdilik boş data hatası fırlatıyoruz (Login/Register dolu döner)
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
