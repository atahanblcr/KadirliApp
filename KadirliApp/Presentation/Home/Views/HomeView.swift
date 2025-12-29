import SwiftUI

struct HomeView: View {
    @StateObject private var viewModel = HomeViewModel()
    @EnvironmentObject var sessionManager: SessionManager // Oturum kontrolü için
    
    // Grid Düzeni: 2 Sütun
    private let columns = [
        GridItem(.flexible(), spacing: 16),
        GridItem(.flexible(), spacing: 16)
    ]
    
    var body: some View {
        NavigationStack(path: $viewModel.navigationPath) {
            ZStack {
                // Arkaplan Rengi
                Color(.systemGroupedBackground)
                    .ignoresSafeArea()
                
                ScrollView {
                    VStack(spacing: 24) {
                        
                        // Hoşgeldin Mesajı
                        if let user = sessionManager.currentUser {
                            HStack {
                                Text("Merhaba, \(user.userMetadata?["full_name"]?.value as? String ?? "Okur")")
                                    .font(.headline)
                                    .foregroundColor(.gray)
                                Spacer()
                            }
                            .padding(.horizontal)
                        }
                        
                        // Ana Grid Menü
                        LazyVGrid(columns: columns, spacing: 16) {
                            ForEach(viewModel.menuOptions) { option in
                                NavigationLink(value: option) {
                                    MenuCardView(option: option)
                                }
                                .buttonStyle(MenuButtonStyle()) // Özel animasyonlu stil
                            }
                        }
                        .padding(.horizontal)
                        .padding(.bottom, 20)
                    }
                    .padding(.top, 10)
                }
            }
            // MARK: - Toolbar / Navigation Bar
            .toolbar {
                // Sol Üst: Logo
                ToolbarItem(placement: .topBarLeading) {
                    HStack(spacing: 8) {
                        ZStack {
                            Circle()
                                .fill(Color.red)
                                .frame(width: 36, height: 36)
                            Text("K")
                                .font(.system(size: 20, weight: .bold, design: .serif))
                                .foregroundColor(.white)
                        }
                        Text("Kadirli Cepte")
                            .font(.title3)
                            .fontWeight(.bold)
                    }
                }
                
                // Sağ Üst: Ayarlar
                ToolbarItem(placement: .topBarTrailing) {
                    Button(action: viewModel.openSettings) {
                        Image(systemName: "gearshape.fill")
                            .foregroundColor(.primary)
                    }
                }
            }
            // MARK: - Navigation Destinations (YÖNLENDİRMELER)
            .navigationDestination(for: HomeMenuOption.self) { option in
                switch option {
                case .transport:
                    TransportView()
                case .taxi:
                    TaxiView()
                case .guide:
                    GuideCategoriesView()
                case .deaths:
                    DeathNoticesView()
                case .pharmacy:
                    DutyPharmacyView()
                case .events:
                    EventCalendarView()
                case .campaigns:
                    CampaignsView()
                case .places:
                    PlacesView()
                case .ads:
                    AdsView()
                }
            }
            // Ayarlar Ekranı (Sheet)
            .sheet(isPresented: $viewModel.showSettings) {
                if let userId = sessionManager.currentUser?.id.uuidString {
                    SettingsView(userId: userId)
                } else {
                    Text("Kullanıcı bilgisi bulunamadı.")
                }
            }
            // 🚨 DÜZELTME BURADA YAPILDI: 🚨
            // .task bloğunu .sheet'in dışına çıkardık. Artık NavigationStack'e bağlı.
            // Ana ekran açılınca taksici mi diye kontrol et
            .task {
                await sessionManager.checkDriverStatus()
            }
        }
    }
}

// MARK: - Subviews
struct MenuCardView: View {
    let option: HomeMenuOption
    
    var body: some View {
        VStack(spacing: 12) {
            Image(systemName: option.iconName)
                .font(.system(size: 40))
                .foregroundColor(.white)
                .frame(width: 70, height: 70)
                .background(Color.white.opacity(0.2))
                .clipShape(Circle())
            
            Text(option.title)
                .font(.headline)
                .fontWeight(.bold)
                .foregroundColor(.white)
                .multilineTextAlignment(.center)
        }
        .frame(maxWidth: .infinity)
        .frame(height: 160)
        .background(option.gradient)
        .cornerRadius(20)
        .shadow(color: option.color.opacity(0.3), radius: 8, x: 0, y: 4)
    }
}
