import Foundation
import SwiftUI
import Combine

enum AppState {
    case loading        // Uygulama açılıyor, kontrol yapılıyor
    case onboarding     // İlk kez açılıyor (Bildirim tercihleri vs.)
    case unauthenticated // Giriş yapılmamış
    case authenticated  // Giriş yapılmış, ana ekran
}

/// Uygulamanın genel oturum durumunu yöneten sınıf.
/// @main (App) dosyasında @StateObject olarak tanımlanıp EnvironmentObject olarak dağıtılmalıdır.
final class SessionManager: ObservableObject {
    
    @Published var currentState: AppState = .loading
    @Published var currentUser: UserDTO?
    
    private let userDefaults = UserDefaults.standard
    private let kIsFirstLaunch = "kIsFirstLaunch"
    private let kAuthToken = "kAuthToken" // Gerçek projede Keychain kullanılmalı!
    
    init() {
        checkSession()
    }
    
    func checkSession() {
        // 1. İlk açılış kontrolü
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
            return
        }
        
        // 2. Token kontrolü (Basitleştirilmiş)
        if let token = userDefaults.string(forKey: kAuthToken), !token.isEmpty {
            // Burada normalde token geçerliliği API ile kontrol edilir.
            // Şimdilik var sayıyoruz.
            currentState = .authenticated
        } else {
            currentState = .unauthenticated
        }
    }
    
    func completeOnboarding() {
        userDefaults.set(false, forKey: kIsFirstLaunch)
        currentState = .unauthenticated // Onboarding bitti, giriş ekranına git
    }
    
    func loginSuccess(user: UserDTO, token: String) {
        // Token'ı kaydet
        userDefaults.set(token, forKey: kAuthToken)
        self.currentUser = user
        
        // Eğer onboarding daha önce yapılmadıysa oraya, yapıldıysa ana ekrana
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
        } else {
            currentState = .authenticated
        }
    }
    
    func logout() {
        userDefaults.removeObject(forKey: kAuthToken)
        currentUser = nil
        currentState = .unauthenticated
    }
}
