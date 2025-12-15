import Foundation
import SwiftUI
import Combine // Düzeltme: Eklendi

@MainActor
final class SettingsViewModel: ObservableObject {
    
    @Published var preferences: NotificationPreferences = .default
    @Published var isLoading = false
    @Published var errorMessage: String?
    
    private let repository: ProfileRepositoryProtocol
    private let userId: String // Oturum açmış kullanıcının ID'si
    
    // Düzeltme: Init parametreleri düzenlendi
    init(repository: ProfileRepositoryProtocol? = nil, userId: String) {
        self.repository = repository ?? ProfileRepository()
        self.userId = userId
    }
    
    func loadPreferences() async {
        isLoading = true
        do {
            // Gerçek uygulamada repository'den çekilir
            // self.preferences = try await repository.getPreferences(userId: userId)
            
            // Simülasyon:
            try await Task.sleep(nanoseconds: 500_000_000)
            self.isLoading = false
        } catch {
            self.errorMessage = error.localizedDescription
            self.isLoading = false
        }
    }
    
    /// Toggle değiştiğinde çağrılır
    func updatePreference() {
        Task {
            do {
                try await repository.updatePreferences(userId: userId, prefs: preferences)
                print("Tercihler güncellendi: \(preferences)")
            } catch {
                self.errorMessage = "Ayarlar kaydedilemedi."
            }
        }
    }
}
