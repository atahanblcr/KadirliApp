import SwiftUI

struct GuideListView: View {
    let category: GuideCategory
    @StateObject private var viewModel = GuideViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            VStack(spacing: 0) {
                // Arama Çubuğu
                HStack {
                    Image(systemName: "magnifyingglass")
                        .foregroundColor(.gray)
                    TextField("Ara...", text: $viewModel.searchText)
                }
                .padding()
                .background(Color(.systemBackground))
                
                // Eğer Muhtarlar ise Filtre Göster
                if category.title.contains("Muhtar") {
                    Picker("Konum", selection: $viewModel.selectedFilter) {
                        Text("Merkez").tag(0)
                        Text("Köy").tag(1)
                    }
                    .pickerStyle(.segmented)
                    .padding()
                    .background(Color(.systemBackground))
                }
                
                ScrollView {
                    LazyVStack(spacing: 12) {
                        if viewModel.state == .loading {
                            ProgressView().padding(.top, 50)
                        } else {
                            let items = viewModel.filteredItems(categoryTitle: category.title)
                            
                            if items.isEmpty {
                                Text("Kayıt bulunamadı.")
                                    .padding(.top, 50)
                                    .foregroundColor(.secondary)
                            } else {
                                ForEach(items) { item in
                                    GuideItemRow(item: item, viewModel: viewModel)
                                }
                            }
                        }
                    }
                    .padding()
                }
            }
        }
        .navigationTitle(category.title)
        .task { await viewModel.loadItems(for: category) }
    }
}

// Satır Tasarımı
struct GuideItemRow: View {
    let item: GuideItem
    @ObservedObject var viewModel: GuideViewModel
    
    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text(item.title)
                    .font(.headline)
                    .foregroundColor(.primary)
                
                if let address = item.address {
                    Text(address)
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .lineLimit(2)
                }
            }
            
            Spacer()
            
            // Butonlar
            HStack(spacing: 12) {
                // Ara
                if item.phone != nil {
                    Button(action: { viewModel.makeCall(phone: item.phone) }) {
                        Image(systemName: "phone.fill")
                            .font(.title3)
                            .padding(10)
                            .background(Color.green.opacity(0.1))
                            .foregroundColor(.green)
                            .clipShape(Circle())
                    }
                }
                
                // Harita
                if item.latitude != nil {
                    Button(action: { viewModel.openMap(lat: item.latitude, long: item.longitude, title: item.title) }) {
                        Image(systemName: "map.fill")
                            .font(.title3)
                            .padding(10)
                            .background(Color.blue.opacity(0.1))
                            .foregroundColor(.blue)
                            .clipShape(Circle())
                    }
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.05), radius: 2, x: 0, y: 1)
    }
}
