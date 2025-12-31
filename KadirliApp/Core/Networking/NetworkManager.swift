import Foundation

/// Uygulamanın ağ trafiğini yöneten Singleton sınıf.
final class NetworkManager {
    
    static let shared = NetworkManager()
    
    private let session: URLSession
    private let decoder: JSONDecoder
    
    // Config dosyasından URL'i çeken kısım
    private var baseURL: String {
        return AppConfig.supabaseUrl
    }
    
    private init() {
        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 30
        self.session = URLSession(configuration: config)
        
        self.decoder = JSONDecoder()
        // Supabase tarih formatı (ISO8601) için ayar
        self.decoder.dateDecodingStrategy = .iso8601
    }
    
    /// Generic API İstek Fonksiyonu
    func request<T: Decodable>(endpoint: Endpoint) async throws -> T {
            
            // 1. URL Hazırlığı
            var fullPath = baseURL + endpoint.path
            
            // Auth ve Storage istekleri "/rest/v1" altında değildir, ana dizindedir.
            // Bu yüzden URL'in sonundaki "/rest/v1" kısmını temizliyoruz.
            if endpoint.path.hasPrefix("/auth") || endpoint.path.hasPrefix("/storage") {
                fullPath = fullPath.replacingOccurrences(of: "/rest/v1", with: "")
            }
            
            guard let url = URL(string: fullPath) else {
                throw AppError.invalidURL
            }
        
        var request = URLRequest(url: url)
        request.httpMethod = endpoint.method.rawValue
        
        // 2. Header Ayarları ve Token
        var headers = endpoint.headers ?? [:]
        
        if let data = KeychainHelper.standard.read(service: "com.atahanblcr.KadirliApp.token", account: "auth_token"),
           let token = String(data: data, encoding: .utf8), !token.isEmpty {
            headers["Authorization"] = "Bearer \(token)"
        }
        
        request.allHTTPHeaderFields = headers
        request.httpBody = endpoint.body
        
        // Debug için konsola URL yazdırıyoruz
        print("🌍 İstek Yapılıyor: \(url.absoluteString)")
        
        do {
            let (data, response) = try await session.data(for: request)
            
            guard let httpResponse = response as? HTTPURLResponse else {
                throw AppError.serverError(statusCode: 0)
            }
            
            // Başarısız İstek Kontrolü
            guard (200...299).contains(httpResponse.statusCode) else {
                if let errorString = String(data: data, encoding: .utf8) {
                    print("❌ Sunucu Hatası: \(errorString)")
                }
                
                // 🚨 EĞER OTURUM SÜRESİ DOLMUŞSA (401 veya JWT Expired)
                if httpResponse.statusCode == 401 || httpResponse.statusCode == 403 {
                    print("⚠️ Oturum süresi doldu, çıkış yapılıyor...")
                    
                    // Tüm uygulamaya haber ver: "Çıkış Yap!"
                    DispatchQueue.main.async {
                        NotificationCenter.default.post(name: NSNotification.Name("ForceLogout"), object: nil)
                    }
                    
                    throw AppError.unauthorized
                }
                
                throw AppError.serverError(statusCode: httpResponse.statusCode)
            }
            
            // Decoding
            do {
                // 👇👇👇 DÜZELTME BURADA 👇👇👇
                // Eğer sunucudan gelen veri boşsa (örn: Update işlemi sonrası 204 dönerse),
                // Decoder'a "null" string'ini veriyoruz. Böylece T tipi Optional ise (String?) nil döner, hata vermez.
                let dataToDecode = data.isEmpty ? "null".data(using: .utf8)! : data
                
                let decodedData = try decoder.decode(T.self, from: dataToDecode)
                return decodedData
            } catch let decodingError as DecodingError {
                print("⚠️ Decoding Hatası: \(decodingError)")
                if let jsonString = String(data: data, encoding: .utf8) {
                    print("Gelen Hatalı Veri: \(jsonString)")
                }
                throw AppError.decodingError(decodingError.localizedDescription)
            }
            
        } catch let error as AppError {
            throw error
        } catch {
            throw AppError.unknown(error)
        }
    }
}
