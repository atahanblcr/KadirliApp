import Foundation
import Combine
import SwiftUI

@MainActor
final class GuideViewModel: ObservableObject {
    @Published var categories: [GuideCategory] = []
    @Published var items: [GuideItem] = []
    
    // Yükleme Durumları
    @Published var state: ViewState = .loading
    
    // Filtreleme (Muhtarlar için)
    @Published var selectedFilter: Int = 0 // 0: Tümü/Merkez, 1: Köy
    @Published var searchText: String = ""
    
    private let repository: GuideRepositoryProtocol
    
    init(repository: GuideRepositoryProtocol? = nil) {
        self.repository = repository ?? GuideRepository()
    }
    
    // Kategorileri Yükle
        func loadCategories() async {
            state = .loading
            do {
                let allCategories = try await repository.fetchCategories()
                
                // 🛠️ DEĞİŞİKLİK: "Taksiler" kategorisini listeden çıkarıyoruz.
                // Çünkü artık o ana ekranda kendi modülü var.
                self.categories = allCategories.filter { !$0.title.contains("Taksi") }
                
                state = .loaded
            } catch {
                state = .error(error.localizedDescription)
            }
        }
    
    // Kategori İçeriğini Yükle
    func loadItems(for category: GuideCategory) async {
        state = .loading
        items = [] // Önce temizle
        do {
            let result = try await repository.fetchItems(categoryId: category.id.uuidString)
            items = result
            state = .loaded
        } catch {
            state = .error(error.localizedDescription)
        }
    }
    
    // Görüntülenecek Liste (Filtreli)
    func filteredItems(categoryTitle: String) -> [GuideItem] {
        // 1. Arama Filtresi
        let searchResult = searchText.isEmpty ? items : items.filter {
            $0.title.localizedCaseInsensitiveContains(searchText)
        }
        
        // 2. Muhtar Ayrımı (Merkez / Köy)
        // Eğer kategori "Muhtar" kelimesi içeriyorsa filtre uygula
        if categoryTitle.contains("Muhtar") {
            if selectedFilter == 0 {
                return searchResult.filter { $0.isCenter } // Merkez
            } else {
                return searchResult.filter { !$0.isCenter } // Köy
            }
        }
        
        return searchResult
    }
    
    // Aksiyonlar
    func makeCall(phone: String?) {
        guard let phone = phone else { return }
        let clean = phone.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
        if let url = URL(string: "tel://\(clean)"), UIApplication.shared.canOpenURL(url) {
            UIApplication.shared.open(url)
        }
    }
    
    func openMap(lat: Double?, long: Double?, title: String) {
        guard let lat = lat, let long = long else { return }
        let urlString = "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m"
        if let url = URL(string: urlString), UIApplication.shared.canOpenURL(url) {
            UIApplication.shared.open(url)
        }
    }
}
