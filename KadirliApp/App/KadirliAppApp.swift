import SwiftUI

@main
struct KadirliAppApp: App {
    // 1. Uygulamanın "Beyni" (SessionManager) burada başlatılıyor
    // @StateObject, uygulama yaşadığı sürece bu objenin hayatta kalmasını sağlar.
    @StateObject private var sessionManager = SessionManager()

    var body: some Scene {
        WindowGroup {
            // 2. Duruma göre hangi ekranın gösterileceğine karar veren "Router"
            Group {
                switch sessionManager.currentState {
                case .loading:
                    // Yükleniyor Ekranı (Splash benzeri)
                    ZStack {
                        Color.white.ignoresSafeArea()
                        ProgressView()
                            .scaleEffect(1.5)
                    }
                    
                case .onboarding:
                    // İlk kez açanlar için Tanıtım Ekranı
                    // (Aşağıda basit bir tane tanımladım)
                    OnboardingView()
                    
                case .unauthenticated:
                    // Giriş Yapılmamışsa -> Login Ekranı
                    LoginView(sessionManager: sessionManager)
                    
                case .authenticated:
                    // Giriş Yapılmışsa -> Ana Ekran
                    HomeView()
                }
            }
            .environmentObject(sessionManager) // 3. SessionManager'ı tüm alt görünümlere dağıt
            .preferredColorScheme(.light) // Şimdilik sadece Açık Tema (Light Mode) zorunlu kıldık
        }
    }
}

// MARK: - Basit Onboarding (Tanıtım) Ekranı
// Bu dosya çok şişmesin diye normalde ayrı dosyada olur ama
// pratik olsun diye buraya ekledim.
struct OnboardingView: View {
    @EnvironmentObject var sessionManager: SessionManager
    
    var body: some View {
        VStack(spacing: 24) {
            Spacer()
            
            Image(systemName: "newspaper.fill")
                .font(.system(size: 100))
                .foregroundColor(.red)
                .padding(.bottom, 20)
            
            Text("Kadirli Cepte")
                .font(.largeTitle)
                .fontWeight(.black)
            
            Text("Haberler, vefat ilanları, nöbetçi eczaneler ve etkinlikler artık tek bir uygulamada.")
                .font(.body)
                .multilineTextAlignment(.center)
                .foregroundColor(.gray)
                .padding(.horizontal, 32)
            
            Spacer()
            
            Button(action: {
                // Tanıtımı geç, giriş ekranına yönlendir
                withAnimation {
                    sessionManager.completeOnboarding()
                }
            }) {
                Text("Hemen Başla")
                    .font(.headline)
                    .fontWeight(.bold)
                    .foregroundColor(.white)
                    .frame(maxWidth: .infinity)
                    .padding()
                    .background(Color.red)
                    .cornerRadius(16)
                    .shadow(radius: 5)
            }
            .padding(.horizontal, 24)
            .padding(.bottom, 40)
        }
    }
}
