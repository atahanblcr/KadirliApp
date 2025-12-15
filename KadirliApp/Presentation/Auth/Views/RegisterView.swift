import SwiftUI

struct RegisterView: View {
    @StateObject private var viewModel = RegisterViewModel()
    @Environment(\.dismiss) var dismiss // Sayfayı kapatmak için
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                VStack(spacing: 24) {
                    
                    // Başlık
                    VStack(spacing: 8) {
                        Image(systemName: "person.badge.plus")
                            .font(.system(size: 60))
                            .foregroundColor(.blue)
                        Text("Aramıza Katıl")
                            .font(.title)
                            .fontWeight(.bold)
                        Text("Kadirli Cepte uygulamasının avantajlarından yararlanmak için hesap oluşturun.")
                            .font(.caption)
                            .foregroundColor(.gray)
                            .multilineTextAlignment(.center)
                            .padding(.horizontal)
                    }
                    .padding(.top, 40)
                    
                    // Form
                    VStack(spacing: 16) {
                        TextField("Ad Soyad", text: $viewModel.fullName)
                            .padding()
                            .background(Color(.systemBackground))
                            .cornerRadius(12)
                        
                        TextField("E-posta", text: $viewModel.email)
                            .keyboardType(.emailAddress)
                            .autocapitalization(.none)
                            .padding()
                            .background(Color(.systemBackground))
                            .cornerRadius(12)
                        
                        SecureField("Şifre (Min 6 karakter)", text: $viewModel.password)
                            .padding()
                            .background(Color(.systemBackground))
                            .cornerRadius(12)
                    }
                    .padding(.horizontal)
                    
                    // Hata Mesajı
                    if let error = viewModel.errorMessage {
                        Text(error)
                            .foregroundColor(.red)
                            .font(.caption)
                            .multilineTextAlignment(.center)
                            .padding(.horizontal)
                    }
                    
                    // Kayıt Butonu
                    Button(action: {
                        Task { await viewModel.register() }
                    }) {
                        if viewModel.isLoading {
                            ProgressView()
                                .tint(.white)
                        } else {
                            Text("Hesap Oluştur")
                                .fontWeight(.bold)
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.blue)
                    .foregroundColor(.white)
                    .cornerRadius(12)
                    .padding(.horizontal)
                    .disabled(viewModel.isLoading)
                    
                    Spacer()
                }
            }
        }
        // Kayıt başarılı olunca uyarı verip giriş sayfasına dön
        .alert("Kayıt Başarılı", isPresented: $viewModel.isSuccess) {
            Button("Giriş Yap") {
                dismiss() // Sayfayı kapatır, LoginView'a döner
            }
        } message: {
            Text("Hesabınız başarıyla oluşturuldu. Şimdi giriş yapabilirsiniz.")
        }
    }
}
