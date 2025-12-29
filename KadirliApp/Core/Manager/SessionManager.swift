import Foundation
import SwiftUI
import Combine

enum AppState {
    case loading        // Uygulama açılıyor
    case onboarding     // İlk kez açılıyor
    case unauthenticated // Giriş yapılmamış
    case authenticated  // Giriş yapılmış
}

final class SessionManager: ObservableObject {
    
    @Published var currentState: AppState = .loading
    @Published var currentUser: UserDTO?
    
    // YENİ: Eğer doluysa bu kişi taksicidir
    @Published var driverTaxiId: String?
    
    private let userDefaults = UserDefaults.standard
    private let kIsFirstLaunch = "kIsFirstLaunch"
    private let kAuthTokenService = "com.atahanblcr.KadirliApp.token"
    
    init() {
        checkSession()
    }
    
    func checkSession() {
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
            return
        }
        
        if let data = KeychainHelper.standard.read(service: kAuthTokenService, account: "auth_token"),
           let token = String(data: data, encoding: .utf8), !token.isEmpty {
            
            print("🔐 Token doğrulandı.")
            
            // Kullanıcı bilgilerini çözümleyip currentUser'a atama işlemi normalde burada yapılır.
            // Şimdilik sadece state'i güncelliyoruz.
            currentState = .authenticated
        } else {
            currentState = .unauthenticated
        }
    }
    
    // YENİ: Taksici mi diye kontrol eden asenkron fonksiyon
    @MainActor
    func checkDriverStatus() async {
        guard let userId = currentUser?.id.uuidString else { return }
        
        let repo = GuideRepository()
        self.driverTaxiId = await repo.getDriverTaxiId(userId: userId)
        
        if let taxiId = self.driverTaxiId {
            print("🚖 SÜRÜCÜ MODU AKTİF! Taksi ID: \(taxiId)")
        } else {
            print("👤 Standart Kullanıcı Modu")
        }
    }
    
    func completeOnboarding() {
        userDefaults.set(false, forKey: kIsFirstLaunch)
        currentState = .unauthenticated
    }
    
    func loginSuccess(user: UserDTO, token: String) {
        if let data = token.data(using: .utf8) {
            KeychainHelper.standard.save(data, service: kAuthTokenService, account: "auth_token")
        }
        
        self.currentUser = user
        
        if userDefaults.object(forKey: kIsFirstLaunch) == nil {
            currentState = .onboarding
        } else {
            currentState = .authenticated
        }
    }
    
    func logout() {
        KeychainHelper.standard.delete(service: kAuthTokenService, account: "auth_token")
        currentUser = nil
        driverTaxiId = nil // Çıkış yapınca taksici yetkisini de sil
        currentState = .unauthenticated
    }
}
