import SwiftUI

struct PhoneLoginView: View {
    @StateObject private var viewModel: AuthViewModel
    
    init(sessionManager: SessionManager) {
        _viewModel = StateObject(wrappedValue: AuthViewModel(sessionManager: sessionManager))
    }
    
    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Spacer()
                
                // Logo ve Başlık
                Image(systemName: "newspaper.fill") // Kendi logonuzu koyabilirsiniz
                    .font(.system(size: 80))
                    .foregroundColor(.red)
                
                Text("Kadirli Cepte")
                    .font(.largeTitle)
                    .fontWeight(.black)
                
                Text("Hoş Geldiniz")
                    .font(.title2)
                    .foregroundColor(.gray)
                
                Spacer()
                
                // Telefon Giriş Alanı
                VStack(alignment: .leading, spacing: 8) {
                    Text("Telefon Numaranız")
                        .font(.caption)
                        .foregroundColor(.gray)
                    
                    HStack {
                        Text("🇹🇷 +90")
                            .fontWeight(.bold)
                            .foregroundColor(.primary)
                            .padding(.leading)
                        
                        TextField("5XX XXX XX XX", text: $viewModel.phoneNumber)
                            .keyboardType(.numberPad)
                            .font(.title3)
                    }
                    .frame(height: 56)
                    .background(Color(.systemGray6))
                    .cornerRadius(12)
                    .overlay(
                        RoundedRectangle(cornerRadius: 12)
                            .stroke(Color.gray.opacity(0.2), lineWidth: 1)
                    )
                }
                .padding(.horizontal)
                
                // KVKK Bilgilendirmesi
                Text("Devam ederek Kullanıcı Sözleşmesi ve Gizlilik Politikası'nı kabul etmiş sayılırsınız.")
                    .font(.caption2)
                    .foregroundColor(.gray)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 40)
                
                // Devam Et Butonu
                Button(action: { Task { await viewModel.sendSMS() } }) {
                    if viewModel.isLoading {
                        ProgressView().tint(.white)
                    } else {
                        Text("Kod Gönder")
                            .fontWeight(.bold)
                            .frame(maxWidth: .infinity)
                    }
                }
                .frame(height: 50)
                .background(Color.red)
                .foregroundColor(.white)
                .cornerRadius(12)
                .padding(.horizontal)
                .disabled(viewModel.isLoading)
                
                Spacer()
            }
            .padding()
            // Yönlendirmeler
            .navigationDestination(isPresented: $viewModel.navigateToOTP) {
                OTPVerificationView(viewModel: viewModel)
            }
            .alert("Hata", isPresented: Binding(get: { viewModel.errorMessage != nil }, set: { _ in viewModel.errorMessage = nil })) {
                Button("Tamam", role: .cancel) { }
            } message: {
                Text(viewModel.errorMessage ?? "")
            }
        }
    }
}
