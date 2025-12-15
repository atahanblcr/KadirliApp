import SwiftUI

struct LoginView: View {
    @StateObject private var viewModel: LoginViewModel
    // SessionManager'ı init içinde değil, EnvironmentObject olarak alıyoruz
    // Ancak ViewModel'e geçirmek için yine de bir değişkene ihtiyaç var,
    // burada pratiklik adına ViewModel'i StateObject olarak tutuyoruz.
    
    init(sessionManager: SessionManager) {
        // ViewModel'i burada oluşturuyoruz
        _viewModel = StateObject(wrappedValue: LoginViewModel(sessionManager: sessionManager))
    }
    
    var body: some View {
        NavigationStack {
            ZStack {
                // Arkaplan
                Color(.systemGroupedBackground).ignoresSafeArea()
                
                VStack(spacing: 24) {
                    
                    // Logo ve Başlık
                    VStack(spacing: 10) {
                        Image(systemName: "newspaper.fill") // Logo placeholder
                            .resizable()
                            .scaledToFit()
                            .frame(width: 80, height: 80)
                            .foregroundColor(.red)
                        
                        Text("Kadirli Cepte")
                            .font(.largeTitle)
                            .fontWeight(.black)
                        
                        Text("Şehrin Nabzı Cebinde")
                            .font(.subheadline)
                            .foregroundColor(.gray)
                    }
                    .padding(.top, 40)
                    .padding(.bottom, 20)
                    
                    // Form Alanları
                    VStack(spacing: 16) {
                        TextField("E-posta Adresi", text: $viewModel.email)
                            .keyboardType(.emailAddress)
                            .autocapitalization(.none)
                            .padding()
                            .background(Color(.systemBackground))
                            .cornerRadius(12)
                        
                        SecureField("Şifre", text: $viewModel.password)
                            .padding()
                            .background(Color(.systemBackground))
                            .cornerRadius(12)
                        
                        HStack {
                            Spacer()
                            Button("Şifremi Unuttum?") {
                                // İleride eklenebilir
                            }
                            .font(.caption)
                            .foregroundColor(.blue)
                        }
                    }
                    .padding(.horizontal)
                    
                    // Giriş Butonu
                    Button(action: {
                        Task { await viewModel.login() }
                    }) {
                        if viewModel.isLoading {
                            ProgressView()
                                .progressViewStyle(CircularProgressViewStyle(tint: .white))
                        } else {
                            Text("Giriş Yap")
                                .fontWeight(.semibold)
                        }
                    }
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.red) // Kadirli teması kırmızı :)
                    .foregroundColor(.white)
                    .cornerRadius(12)
                    .padding(.horizontal)
                    .disabled(viewModel.isLoading)
                    
                    Spacer()
                    
                    // Kayıt Ol Yönlendirmesi
                    HStack {
                        Text("Hesabın yok mu?")
                            .foregroundColor(.gray)
                        
                        // İŞTE BURASI DEĞİŞTİ: NavigationLink Eklendi
                        NavigationLink(destination: RegisterView()) {
                            Text("Kayıt Ol")
                                .fontWeight(.bold)
                                .foregroundColor(.red)
                        }
                    }
                    .padding(.bottom, 20)
                }
            }
            .alert(isPresented: $viewModel.showError) {
                Alert(title: Text("Hata"), message: Text(viewModel.errorMessage ?? ""), dismissButton: .default(Text("Tamam")))
            }
        }
    }
}
