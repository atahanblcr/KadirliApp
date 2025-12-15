import SwiftUI

struct DutyPharmacyView: View {
    @StateObject private var viewModel = PharmacyViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVStack(spacing: 20, pinnedViews: [.sectionHeaders]) {
                    
                    switch viewModel.state {
                    case .loading:
                        ProgressView("Eczaneler Yükleniyor...")
                            .padding(.top, 50)
                        
                    case .empty:
                        Text("Nöbetçi eczane bilgisi bulunamadı.")
                            .foregroundColor(.secondary)
                            .padding(.top, 50)
                        
                    case .error(let message):
                        Text("Hata: \(message)").foregroundColor(.red).padding()
                        
                    case .loaded:
                        // --- BÖLÜM 1: GÜNCEL NÖBETÇİLER ---
                        if !viewModel.currentPharmacies.isEmpty {
                            // Dinamik Başlık Hesabı
                            Section(header: RegionHeaderView(title: getCurrentHeaderTitle())) {
                                ForEach(viewModel.currentPharmacies) { pharmacy in
                                    PharmacyCardView(
                                        pharmacy: pharmacy,
                                        onCall: { viewModel.callPharmacy(phone: pharmacy.phone) },
                                        onMap: { viewModel.openMap(lat: pharmacy.latitude, long: pharmacy.longitude, name: pharmacy.name) }
                                    )
                                    .padding(.horizontal)
                                }
                            }
                        }
                        
                        // --- BÖLÜM 2: SIRADAKİ NÖBETÇİLER ---
                        if !viewModel.nextPharmacies.isEmpty {
                            Section(header: RegionHeaderView(title: "SIRADAKİ NÖBETÇİ (Yarın)")) {
                                ForEach(viewModel.nextPharmacies) { pharmacy in
                                    PharmacyCardView(
                                        pharmacy: pharmacy,
                                        onCall: { viewModel.callPharmacy(phone: pharmacy.phone) },
                                        onMap: { viewModel.openMap(lat: pharmacy.latitude, long: pharmacy.longitude, name: pharmacy.name) }
                                    )
                                    .padding(.horizontal)
                                    .opacity(0.8)
                                }
                            }
                        }
                    }
                }
                .padding(.bottom, 20)
            }
        }
        .navigationTitle("Nöbetçi Eczaneler")
        .navigationBarTitleDisplayMode(.inline)
        .task { await viewModel.loadPharmacies() }
    }
    
    // İstenilen Dinamik Başlık Mantığı
    func getCurrentHeaderTitle() -> String {
        let hour = Calendar.current.component(.hour, from: Date())
        
        if hour >= 0 && hour < 9 {
            // Gece 00:00 - 09:00 arası
            return "Sabah 09:00'a kadar nöbetçiler"
        } else {
            // Gün içi (09:00 - 23:59)
            return "Bugün akşam saat 19.00'dan yarın sabah 09.00'a kadar nöbetçiler"
        }
    }
}

// Başlık Tasarımı (Section Header)
struct RegionHeaderView: View {
    let title: String
    var body: some View {
        HStack {
            Text(title)
                .font(.caption) // Biraz daha küçük puntolu şık durur
                .fontWeight(.bold)
                .foregroundColor(.secondary)
                .padding(.vertical, 8)
                .padding(.horizontal, 16)
                .multilineTextAlignment(.leading) // Uzun metinler için
            Spacer()
        }
        .background(Color(.systemGroupedBackground))
    }
}
