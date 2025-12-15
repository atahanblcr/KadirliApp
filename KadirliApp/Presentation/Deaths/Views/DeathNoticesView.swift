import SwiftUI

struct DeathNoticesView: View {
    @StateObject private var viewModel = DeathNoticesViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            VStack(spacing: 0) {
                // 1. Tarih Filtresi (Yatay Scroll)
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 12) {
                        ForEach(viewModel.availableDates, id: \.self) { date in
                            DateFilterPill(
                                title: viewModel.formatDateForFilter(date),
                                isSelected: Calendar.current.isDate(date, inSameDayAs: viewModel.selectedDate)
                            ) {
                                // Tarih değişince veriyi yeniden yükle
                                viewModel.selectedDate = date
                                Task { await viewModel.loadDeaths() }
                            }
                        }
                    }
                    .padding()
                }
                .background(Color(.systemBackground))
                
                // 2. Liste
                ScrollView {
                    LazyVStack(spacing: 16) {
                        switch viewModel.state {
                        case .loading:
                            ProgressView("Yükleniyor...")
                                .padding(.top, 50)
                            
                        case .empty:
                            VStack(spacing: 16) {
                                Image(systemName: "text.book.closed")
                                    .font(.system(size: 50))
                                    .foregroundColor(.gray)
                                Text("Seçilen tarihte vefat ilanı bulunmamaktadır.")
                                    .font(.subheadline)
                                    .foregroundColor(.secondary)
                            }
                            .padding(.top, 80)
                            
                        case .loaded:
                            ForEach(viewModel.deaths) { death in
                                DeathCardView(death: death) {
                                    // Harita butonuna tıklanınca
                                    viewModel.openMap(
                                        lat: death.latitude,
                                        long: death.longitude,
                                        name: death.condolenceAddress ?? "Konum"
                                    )
                                }
                                .padding(.horizontal)
                            }
                            
                        case .error(let message):
                            Text("Hata: \(message)")
                                .foregroundColor(.red)
                                .padding()
                        }
                    }
                    .padding(.vertical)
                }
            }
        }
        .navigationTitle("Vefat İlanları")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            // İlk açılışta veri çek
            await viewModel.loadDeaths()
        }
    }
}

// Yardımcı Bileşen: Tarih Filtre Hapı
struct DateFilterPill: View {
    let title: String
    let isSelected: Bool
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            Text(title)
                .font(.subheadline)
                .fontWeight(isSelected ? .bold : .regular)
                .padding(.horizontal, 16)
                .padding(.vertical, 8)
                .background(isSelected ? Color.black : Color(.systemGray5))
                .foregroundColor(isSelected ? .white : .primary)
                .cornerRadius(20)
                .animation(.easeInOut(duration: 0.2), value: isSelected)
        }
    }
}

