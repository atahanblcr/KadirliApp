import Foundation
import SwiftUI
import Combine

@MainActor
final class AuthViewModel: ObservableObject {
    
    // Durumlar
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var navigateToOTP = false
    @Published var navigateToProfile = false
    @Published var isSuccess = false // Ana ekrana geçiş için
    
    // Veriler
    @Published var phoneNumber = ""
    @Published var otpCode = ""
    @Published var username = "" // İsim yerine Kullanıcı Adı
    @Published var selectedLocationType = 0 // 0: Mahalle, 1: Köy
    @Published var selectedLocation = ""
    
    // İzinler
    @Published var isTermsAccepted = false
    @Published var isMarketingAccepted = false
    
    // Geçici Hafıza
    private var tempUser: UserDTO?
    private var tempToken: String?
    
    private let authRepository: AuthRepositoryProtocol
    private let sessionManager: SessionManager
    
    init(authRepository: AuthRepositoryProtocol? = nil, sessionManager: SessionManager) {
        self.authRepository = authRepository ?? AuthRepository()
        self.sessionManager = sessionManager
    }
    
    // 1. SMS Gönder Kısmı
        func sendSMS() async {
            guard validatePhone() else { return }
            
            isLoading = true
            errorMessage = nil
            
            do {
                // Temizlik: Boşlukları sil
                var cleanPhone = phoneNumber.replacingOccurrences(of: " ", with: "")
                
                // Başındaki 0'ı sil (Örn: 0555 -> 555)
                if cleanPhone.hasPrefix("0") {
                    cleanPhone = String(cleanPhone.dropFirst())
                }
                
                // Formatlama:
                // 1. Önce numaranın başına eksikse 90 ekle
                let phoneWithCountryCode = cleanPhone.hasPrefix("90") ? cleanPhone : "90\(cleanPhone)"
                
                // 2. SONRA MUTLAKA BAŞINA "+" EKLE (Supabase API'si için şart)
                let finalPhoneToSend = "+\(phoneWithCountryCode)"
                
                // LOG: Konsolda başında + olduğunu teyit edelim
                print("📡 SMS İsteği Gönderiliyor (Formatlı): \(finalPhoneToSend)")
                
                try await authRepository.sendOTP(phone: finalPhoneToSend)
                
                self.navigateToOTP = true
            } catch {
                print("❌ SMS Hatası: \(error)")
                self.errorMessage = "Kod gönderilemedi: \(error.localizedDescription)"
            }
            isLoading = false
        }

        // 2. Doğrulama Kısmı (Aynı mantık)
        func verifyCode() async {
            guard otpCode.count == 6 else { return }
            isLoading = true
            
            do {
                // Aynı temizlik işlemleri
                var cleanPhone = phoneNumber.replacingOccurrences(of: " ", with: "")
                if cleanPhone.hasPrefix("0") { cleanPhone = String(cleanPhone.dropFirst()) }
                
                let phoneWithCountryCode = cleanPhone.hasPrefix("90") ? cleanPhone : "90\(cleanPhone)"
                
                // BURADA DA MUTLAKA "+" EKLİYORUZ
                let finalPhoneToSend = "+\(phoneWithCountryCode)"
                
                print("📡 Doğrulama Yapılıyor: \(finalPhoneToSend) - Kod: \(otpCode)")
                
                let response = try await authRepository.verifyOTP(phone: finalPhoneToSend, token: otpCode)
                print("✅ Doğrulama Başarılı!")
                
                // ... (Giriş başarılı işlemleri aynen kalacak) ...
                 if let name = response.user.userMetadata?["full_name"]?.value as? String, !name.isEmpty {
                    sessionManager.loginSuccess(user: response.user, token: response.accessToken)
                    self.isSuccess = true
                } else {
                    self.tempUser = response.user
                    self.tempToken = response.accessToken
                    self.navigateToProfile = true
                }

            } catch {
                print("❌ Doğrulama Hatası: \(error)")
                self.errorMessage = "Kod hatalı veya süresi dolmuş."
            }
            isLoading = false
        }
    
    // 3. Profili Kaydet
    func completeProfile() async {
        guard !username.isEmpty, !selectedLocation.isEmpty else {
            errorMessage = "Lütfen tüm alanları doldurun."
            return
        }
        guard isTermsAccepted else {
            errorMessage = "Lütfen Kullanım Koşullarını kabul edin."
            return
        }
        
        isLoading = true
        
        do {
            let userId = tempUser?.id.uuidString ?? sessionManager.currentUser?.id.uuidString
            
            guard let uid = userId else {
                errorMessage = "Kullanıcı bilgisi bulunamadı."
                isLoading = false
                return
            }
            
            // ⚡️ KRİTİK ADIM: Token'ı geçici olarak kaydet (NetworkManager kullanabilsin diye)
            if let token = tempToken, let data = token.data(using: .utf8) {
                KeychainHelper.standard.save(data, service: "com.atahanblcr.KadirliApp.token", account: "auth_token")
                print("⚡️ Token güncelleme için kaydedildi.")
            }
            
            // Şimdi güncelleme isteği at
            try await authRepository.updateProfile(
                userId: uid,
                fullName: username,
                neighborhood: selectedLocation
            )
            
            // İşlem bitince resmi oturum açılışını yap
            if let user = tempUser, let token = tempToken {
                sessionManager.loginSuccess(user: user, token: token)
            }
            
            self.isSuccess = true
            
        } catch {
            print("❌ Profil Hatası: \(error)")
            // Hata olursa token'ı temizle
            KeychainHelper.standard.delete(service: "com.atahanblcr.KadirliApp.token", account: "auth_token")
            self.errorMessage = "Profil kaydedilemedi: \(error.localizedDescription)"
        }
        isLoading = false
    }
    
    // Yardımcı: Telefon doğrulama
    private func validatePhone() -> Bool {
        if phoneNumber.count < 10 {
            errorMessage = "Lütfen geçerli bir numara girin."
            return false
        }
        return true
    }
}
