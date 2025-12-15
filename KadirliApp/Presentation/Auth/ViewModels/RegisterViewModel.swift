import Foundation
import SwiftUI
import Combine

@MainActor
final class RegisterViewModel: ObservableObject {
    
    @Published var fullName = ""
    @Published var email = ""
    @Published var password = ""
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var isSuccess = false
    
    private let authRepository: AuthRepositoryProtocol
    
    init(authRepository: AuthRepositoryProtocol? = nil) {
        self.authRepository = authRepository ?? AuthRepository()
    }
    
    func register() async {
        guard validate() else { return }
        
        isLoading = true
        errorMessage = nil
        
        do {
            // Repository üzerinden kayıt işlemini başlat
            let _ = try await authRepository.register(email: email, pass: password, fullName: fullName)
            
            // Başarılı olursa
            self.isSuccess = true
            self.isLoading = false
            
        } catch {
            self.errorMessage = "Kayıt başarısız: \(error.localizedDescription)"
            self.isLoading = false
        }
    }
    
    private func validate() -> Bool {
        if fullName.isEmpty || email.isEmpty || password.isEmpty {
            errorMessage = "Lütfen tüm alanları doldurun."
            return false
        }
        if password.count < 6 {
            errorMessage = "Şifre en az 6 karakter olmalıdır."
            return false
        }
        return true
    }
}
