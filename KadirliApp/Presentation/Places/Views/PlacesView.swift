import SwiftUI

struct PlacesView: View {
    @StateObject private var viewModel = PlacesViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                VStack(spacing: 20) {
                    // FİLTRE BUTONU (ÜSTTE)
                    HStack {
                        Spacer()
                        Button(action: {
                            withAnimation { viewModel.sortNearest.toggle() }
                        }) {
                            HStack {
                                Image(systemName: "arrow.up.arrow.down")
                                Text(viewModel.sortNearest ? "Sıralama: Yakından Uzağa" : "Sıralama: A-Z")
                            }
                            .font(.caption)
                            .fontWeight(.bold)
                            .padding(.horizontal, 12)
                            .padding(.vertical, 8)
                            .background(Color.white)
                            .cornerRadius(20)
                            .shadow(radius: 1)
                        }
                        .padding(.trailing)
                    }
                    
                    LazyVStack(spacing: 20) {
                        switch viewModel.state {
                        case .loading:
                            ProgressView("Yükleniyor...")
                                .padding(.top, 50)
                            
                        case .loaded:
                            if viewModel.places.isEmpty {
                                Text("Gezilecek yer bulunamadı.")
                                    .padding(.top, 50)
                                    .foregroundColor(.secondary)
                            } else {
                                ForEach(viewModel.places) { place in
                                    NavigationLink(destination: PlaceDetailView(place: place)) {
                                        PlaceCard(place: place)
                                    }
                                    .buttonStyle(PlainButtonStyle())
                                }
                            }
                            
                        case .empty:
                            Text("Gezilecek yer bulunamadı.")
                                .padding(.top, 50)
                                .foregroundColor(.secondary)
                            
                        case .error(let msg):
                            Text(msg)
                                .foregroundColor(.red)
                                .padding()
                        }
                    }
                    .padding(.horizontal)
                }
                .padding(.top)
            }
        }
        .navigationTitle("Gezilecek Yerler")
        .task { await viewModel.loadPlaces() }
    }
}

// MARK: - PlaceCard (Kayıp Parça Burasıydı)
struct PlaceCard: View {
    let place: Place
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Görsel
            if let url = place.imageUrls?.first {
                AsyncImage(url: URL(string: url)) { img in
                    img.resizable().aspectRatio(contentMode: .fill)
                } placeholder: {
                    Color.gray.opacity(0.2)
                }
                .frame(height: 200)
                .clipped()
            }
            
            HStack {
                Text(place.title)
                    .font(.headline)
                    .fontWeight(.bold)
                
                Spacer()
                
                // Manuel Mesafe Bilgisi (V3)
                if let dist = place.distanceText {
                    Label(dist, systemImage: "mappin.and.ellipse")
                        .font(.caption)
                        .padding(6)
                        .background(Color.cyan.opacity(0.1))
                        .foregroundColor(.cyan)
                        .cornerRadius(8)
                }
            }
            .padding()
        }
        .background(Color.white)
        .cornerRadius(16)
        .shadow(radius: 2)
    }
}
