import SwiftUI

struct AdsView: View {
    @StateObject private var viewModel = AdsViewModel()
    @State private var showAddAdSheet = false // Yeni: Formu açmak için state
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            // MEVCUT LİSTE YAPISI
            ScrollView {
                LazyVStack(spacing: 16) {
                    
                    // Kategori Filtresi Bilgisi
                    if let category = viewModel.selectedCategory {
                        HStack {
                            Text("Filtre:")
                                .foregroundColor(.secondary)
                            Text(category.displayName)
                                .fontWeight(.bold)
                                .foregroundColor(category.color)
                            Spacer()
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
                .padding(.bottom, 80) // FAB altında kalmasın diye boşluk
            }
            
            // YENİ EKLENEN FAB (FLOATING ACTION BUTTON)
            VStack {
                Spacer()
                HStack {
                    Spacer()
                    Button(action: { showAddAdSheet = true }) {
                        HStack {
                            Image(systemName: "plus")
                                .font(.title2)
                            Text("İlan Ver")
                                .fontWeight(.bold)
                        }
                        .padding()
                        .background(Color.red)
                        .foregroundColor(.white)
                        .cornerRadius(30)
                        .shadow(color: Color.black.opacity(0.3), radius: 4, x: 0, y: 4)
                    }
                    .padding(.trailing, 20)
                    .padding(.bottom, 20)
                }
            }
        }
        .navigationTitle("Seri İlanlar")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Button(action: { viewModel.selectCategory(nil) }) {
                        Label("Tümü", systemImage: "square.grid.2x2")
                    }
                    Divider()
                    ForEach(AdType.allCases, id: \.self) { type in
                        Button(action: { viewModel.selectCategory(type) }) {
                            Label(type.displayName, systemImage: "circle.fill")
                        }
                    }
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: "line.3.horizontal.decrease.circle")
                        Text("Filtrele")
                    }
                    .font(.subheadline)
                    .fontWeight(.medium)
                }
            }
        }
        .sheet(isPresented: $showAddAdSheet) {
            AddAdView(viewModel: viewModel)
        }
        .task { await viewModel.loadAds() }
    }
}
