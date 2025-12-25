import Foundation
import SwiftUI
import Combine

enum AppState {
    case loading        // Uygulama açılıyor, kontrol yapılıyor
    case onboarding     // İlk kez açılıyor
    case unauthenticated // Giriş yapılmamış
    case authenticated  // Giriş yapılmış, ana ekran
}

final class SessionManager: ObservableObject {
    
    @Published var currentState: AppState = .loading
    @Published var currentUser: UserDTO?
    
    private let userDefaults = UserDefaults.standard
    private let kIsFirstLaunch = "kIsFirstLaunch"
    
    // YENİ: Token servisi için bir isim (Keychain'de karışıklık olmasın diye)
    private let kAuthTokenService = "com.atahanblcr.KadirliApp.token"
    
    init() {
        checkSession()
    }
    
    func checkSession() {
        // 1. İlk açılış kontrolü (Burası hala UserDefaults, çünkü güvenlik riski yok)
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
            return
        }
        
        // 2. Token kontrolü (ARTIK KEYCHAIN'DEN OKUYORUZ)
        if let data = KeychainHelper.standard.read(service: kAuthTokenService, account: "auth_token"),
           let token = String(data: data, encoding: .utf8), !token.isEmpty {
            
            // İstersen burada token'ı konsola yazdırıp test edebilirsin (Release'de silersin)
            print("🔐 Token Keychain'den okundu.")
            currentState = .authenticated
        } else {
            currentState = .unauthenticated
        }
    }
    
    func completeOnboarding() {
        userDefaults.set(false, forKey: kIsFirstLaunch)
        currentState = .unauthenticated
    }
    
    func loginSuccess(user: UserDTO, token: String) {
        // YENİ: Token'ı güvenli kasaya (Keychain) kaydediyoruz
        if let data = token.data(using: .utf8) {
            KeychainHelper.standard.save(data, service: kAuthTokenService, account: "auth_token")
            print("💾 Token Keychain'e kaydedildi.")
        }
        
        self.currentUser = user
        
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
        } else {
            currentState = .authenticated
        }
    }
    
    func logout() {
        // YENİ: Çıkış yapınca kasadan siliyoruz
        KeychainHelper.standard.delete(service: kAuthTokenService, account: "auth_token")
        currentUser = nil
        currentState = .unauthenticated
    }
}
