import SwiftUI

struct OTPVerificationView: View {
    @ObservedObject var viewModel: AuthViewModel
    @FocusState private var isFocused: Bool
    
    var body: some View {
        VStack(spacing: 24) {
            Spacer()
            
            Text("Doğrulama Kodu")
                .font(.title)
                .fontWeight(.bold)
            
            Text("+90 \(viewModel.phoneNumber) numarasına gönderilen 6 haneli kodu giriniz.")
                .font(.body)
                .foregroundColor(.gray)
                .multilineTextAlignment(.center)
                .padding(.horizontal)
            
            // OTP Input (Basitleştirilmiş)
            TextField("Kod", text: $viewModel.otpCode)
                .keyboardType(.numberPad)
                .textContentType(.oneTimeCode) // iOS otomatik doldurma için
                .font(.system(size: 32, weight: .bold, design: .monospaced))
                .multilineTextAlignment(.center)
                .padding()
                .background(Color(.systemGray6))
                .cornerRadius(12)
                .padding(.horizontal, 40)
                .focused($isFocused)
                .onChange(of: viewModel.otpCode) { newValue in
                    if newValue.count == 6 {
                        Task { await viewModel.verifyCode() }
                    }
                }
            
            if viewModel.isLoading {
                ProgressView("Doğrulanıyor...")
            }
            
            Spacer()
        }
        .onAppear { isFocused = true }
        // Profil sayfasına yönlendirme (Eğer yeni kullanıcıysa)
        .navigationDestination(isPresented: $viewModel.navigateToProfile) {
            ProfileCreationView(viewModel: viewModel)
        }
    }
}

