import Foundation

protocol ProfileRepositoryProtocol {
    func getPreferences(userId: String) async throws -> NotificationPreferences
    func updatePreferences(userId: String, prefs: NotificationPreferences) async throws
}

final class ProfileRepository: ProfileRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func getPreferences(userId: String) async throws -> NotificationPreferences {
        // Supabase'den gelen veri [Profile] dizisidir, ilkini alırız.
        // Not: ProfileResponseDTO oluşturulmalı, burada basitleştirilmiş yapı kullanıyorum.
        // Gerçekte: Profile entity içindeki JSONB parse edilmeli.
        
        // Mocking logic for simplicity in this context:
        // Normalde NetworkManager generic request ile tüm profili çeker,
        // biz içinden preferences'ı alırız.
        return .default // Placeholder
    }
    
    func updatePreferences(userId: String, prefs: NotificationPreferences) async throws {
        // PATCH isteği dönüş değeri genellikle boştur veya güncellenen kayıttır.
        // Generic yapımızda Void dönüşü için düzenleme gerekebilir, şimdilik IgnoreResponse varsayalım.
        let _: String? = try? await networkManager.request(endpoint: ProfileEndpoint.updatePreferences(userId: userId, prefs: prefs))
    }
}
