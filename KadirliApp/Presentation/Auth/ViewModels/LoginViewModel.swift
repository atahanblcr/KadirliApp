import Foundation
import Combine

@MainActor
final class LoginViewModel: ObservableObject {
    
    // MARK: - Properties
    @Published var email = ""
    @Published var password = ""
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var showError = false
    
    private let authRepository: AuthRepositoryProtocol
    private let sessionManager: SessionManager
    
    // Dependency Injection
        // Değişiklik: authRepository'yi parantez içinde değil, içeride oluşturuyoruz.
        init(authRepository: AuthRepositoryProtocol? = nil, sessionManager: SessionManager) {
            self.authRepository = authRepository ?? AuthRepository()
            self.sessionManager = sessionManager
        }
    
    // MARK: - Actions
    func login() async {
        guard validate() else { return }
        
        isLoading = true
        errorMessage = nil
        
        do {
            let response = try await authRepository.login(email: email, pass: password)
            
            // Başarılı giriş
            sessionManager.loginSuccess(user: response.user, token: response.accessToken)
            
        } catch {
            self.errorMessage = error.localizedDescription
            self.showError = true
        }
        
        isLoading = false
    }
    
    func loginWithGoogle() {
        // Mock Implementation
        isLoading = true
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
            self.isLoading = false
            // Mock User
            // self.sessionManager.loginSuccess(...)
        }
    }
    
    func loginWithApple() {
        // Mock Implementation
    }
    
    // MARK: - Helpers
    private func validate() -> Bool {
        if email.isEmpty || password.isEmpty {
            errorMessage = "Lütfen tüm alanları doldurun."
            showError = true
            return false
        }
        // Basit email regex
        let emailRegEx = "[A-Z0-9a-z._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,64}"
        let emailPred = NSPredicate(format:"SELF MATCHES %@", emailRegEx)
        if !emailPred.evaluate(with: email) {
            errorMessage = "Geçerli bir e-posta adresi girin."
            showError = true
            return false
        }
        return true
    }
}

