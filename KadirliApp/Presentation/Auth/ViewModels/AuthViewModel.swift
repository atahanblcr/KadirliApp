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
    @Published var fullName = ""
    @Published var selectedLocationType = 0 // 0: Mahalle, 1: Köy
    @Published var selectedLocation = ""
    
    // İzinler
    @Published var isTermsAccepted = false // Kullanım Koşulları
    @Published var isMarketingAccepted = false // Ticari İleti (İsteğe bağlı)
    
    private let authRepository: AuthRepositoryProtocol
    private let sessionManager: SessionManager
    
    init(authRepository: AuthRepositoryProtocol? = nil, sessionManager: SessionManager) {
        self.authRepository = authRepository ?? AuthRepository()
        self.sessionManager = sessionManager
    }
    
    // 1. SMS Gönder
    func sendSMS() async {
        guard validatePhone() else { return }
        
        isLoading = true
        errorMessage = nil
        
        do {
            // +90 formatı ekle (eğer kullanıcı girmediyse)
            let formattedPhone = phoneNumber.starts(with: "+90") ? phoneNumber : "+90\(phoneNumber)"
            try await authRepository.sendOTP(phone: formattedPhone)
            
            // Başarılıysa OTP ekranına geç
            self.navigateToOTP = true
        } catch {
            self.errorMessage = "Kod gönderilemedi: \(error.localizedDescription)"
        }
        isLoading = false
    }
    
    // 2. Kodu Doğrula
    func verifyCode() async {
        guard otpCode.count == 6 else {
            errorMessage = "Lütfen 6 haneli kodu eksiksiz girin."
            return
        }
        
        isLoading = true
        errorMessage = nil
        
        do {
            let formattedPhone = phoneNumber.starts(with: "+90") ? phoneNumber : "+90\(phoneNumber)"
            let response = try await authRepository.verifyOTP(phone: formattedPhone, token: otpCode)
            
            // Token'ı kaydet (Oturum açıldı)
            sessionManager.loginSuccess(user: response.user, token: response.accessToken)
            
            // Kontrol: Kullanıcı yeni mi eski mi?
            // (Burada isim doluysa eski kullanıcıdır diyebiliriz)
            if let name = response.user.userMetadata?["full_name"]?.value as? String, !name.isEmpty {
                // Eski kullanıcı -> Ana Sayfaya
                self.isSuccess = true
            } else {
                // Yeni kullanıcı -> Profil Tamamlamaya
                self.navigateToProfile = true
            }
            
        } catch {
            self.errorMessage = "Kod hatalı veya süresi dolmuş."
        }
        isLoading = false
    }
    
    // 3. Profili Kaydet
    func completeProfile() async {
        guard !fullName.isEmpty, !selectedLocation.isEmpty else {
            errorMessage = "Lütfen tüm alanları doldurun."
            return
        }
        guard isTermsAccepted else {
            errorMessage = "Lütfen Kullanım Koşullarını kabul edin."
            return
        }
        
        isLoading = true
        
        do {
            // Mevcut kullanıcı ID'sini al
            guard let userId = sessionManager.currentUser?.id.uuidString else { return }
            
            try await authRepository.updateProfile(
                userId: userId,
                fullName: fullName,
                neighborhood: selectedLocation
            )
            
            // Her şey bitti -> Ana Sayfaya
            self.isSuccess = true
            
        } catch {
            self.errorMessage = "Profil kaydedilemedi."
        }
        isLoading = false
    }
    
    private func validatePhone() -> Bool {
        // Basit kontrol (5XX...)
        if phoneNumber.count < 10 {
            errorMessage = "Lütfen geçerli bir numara girin."
            return false
        }
        return true
    }
}

