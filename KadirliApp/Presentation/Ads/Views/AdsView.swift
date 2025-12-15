import SwiftUI

struct AdsView: View {
    @StateObject private var viewModel = AdsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVStack(spacing: 16) {
                    
                    // Seçili Kategori Bilgilendirmesi (Opsiyonel: Kullanıcı neyi seçtiğini görsün)
                    if let category = viewModel.selectedCategory {
                        HStack {
                            Text("Filtre:")
                                .foregroundColor(.secondary)
                            Text(category.displayName)
                                .fontWeight(.bold)
                                .foregroundColor(category.color)
                            
                            Spacer()
                            
                            // Filtreyi Temizle Butonu (Küçük X)
                            Button(action: { viewModel.selectCategory(nil) }) {
                                Image(systemName: "xmark.circle.fill")
                                    .foregroundColor(.gray)
                            }
                        }
                        .padding(.horizontal)
                        .padding(.top, 10)
                    }
                    
                    switch viewModel.state {
                    case .loading:
                        ProgressView("İlanlar Yükleniyor...")
                            .padding(.top, 50)
                        
                    case .loaded:
                        if viewModel.filteredAds.isEmpty {
                            VStack(spacing: 12) {
                                Image(systemName: "magnifyingglass")
                                    .font(.system(size: 50))
                                    .foregroundColor(.gray)
                                Text("Bu kategoride ilan bulunamadı.")
                                    .foregroundColor(.secondary)
                            }
                            .padding(.top, 50)
                        } else {
                            ForEach(viewModel.filteredAds) { ad in
                                NavigationLink(destination: AdDetailView(ad: ad, viewModel: viewModel)) {
                                    AdCardView(ad: ad)
                                }
                                .buttonStyle(PlainButtonStyle())
                                .padding(.horizontal)
                            }
                        }
                        
                    case .empty:
                        Text("Henüz hiç ilan yok.")
                            .padding(.top, 50)
                            .foregroundColor(.secondary)
                        
                    case .error(let message):
                        Text("Hata: \(message)")
                            .foregroundColor(.red)
                            .padding()
                    }
                }
                .padding(.bottom, 20)
            }
        }
        .navigationTitle("Seri İlanlar") // Standart Başlık (Geri butonu için şart)
        .navigationBarTitleDisplayMode(.inline)
        // SAĞ ÜST KÖŞEYE FİLTRE MENÜSÜ EKLİYORUZ
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    // "Tümü" Seçeneği
                    Button(action: { viewModel.selectCategory(nil) }) {
                        Label("Tümü", systemImage: "square.grid.2x2")
                    }
                    
                    Divider()
                    
                    // Diğer Kategoriler
                    ForEach(AdType.allCases, id: \.self) { type in
                        Button(action: { viewModel.selectCategory(type) }) {
                            Label(type.displayName, systemImage: "circle.fill")
                        }
                    }
                } label: {
                    // Butonun Görünümü
                    HStack(spacing: 4) {
                        Image(systemName: "line.3.horizontal.decrease.circle")
                        Text("Filtrele")
                    }
                    .font(.subheadline)
                    .fontWeight(.medium)
                }
            }
        }
        .task { await viewModel.loadAds() }
    }
}
