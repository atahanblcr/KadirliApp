import Foundation

// 1. Protokol (Kurallar)
protocol AuthRepositoryProtocol {
    func sendOTP(phone: String) async throws
    func verifyOTP(phone: String, token: String) async throws -> AuthResponseDTO
    func updateProfile(userId: String, fullName: String, neighborhood: String) async throws
    func signOut() async throws
}

// 2. Sınıf (Uygulama)
final class AuthRepository: AuthRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    // Telefon Numarasına SMS Gönder
    func sendOTP(phone: String) async throws {
        let endpoint = AuthEndpoint.sendOTP(phone: phone)
        // Dönüş tipi olmadığı için (Void) response'u atlıyoruz
        let _: String? = try? await networkManager.request(endpoint: endpoint)
    }
    
    // Gelen Kodu Doğrula
    func verifyOTP(phone: String, token: String) async throws -> AuthResponseDTO {
        let endpoint = AuthEndpoint.verifyOTP(phone: phone, token: token)
        return try await networkManager.request(endpoint: endpoint)
    }
    
    // Profil Bilgilerini Güncelle
    func updateProfile(userId: String, fullName: String, neighborhood: String) async throws {
        let endpoint = AuthEndpoint.updateProfile(userId: userId, fullName: fullName, neighborhood: neighborhood)
        let _: String? = try await networkManager.request(endpoint: endpoint)
    }
    
    // Çıkış Yap
    func signOut() async throws {
        // Backend tarafında logout işlemi gerekirse buraya eklenir
    }
}
