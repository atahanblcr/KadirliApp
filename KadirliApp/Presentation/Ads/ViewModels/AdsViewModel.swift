import Foundation
import SwiftUI // UIKit yerine SwiftUI daha uygun ama ikisi de olur
import Combine // Düzeltme 1

@MainActor
final class AdsViewModel: ObservableObject {
    
    @Published var state: ViewState = .loading
    @Published var selectedCategory: AdType? = nil // nil = Tümü
    
    private var allAds: [Ad] = []
    @Published var filteredAds: [Ad] = []
    
    private let repository: AdsRepositoryProtocol
    
    // Düzeltme 2: Init hatası giderildi
    init(repository: AdsRepositoryProtocol? = nil) {
        self.repository = repository ?? AdsRepository()
    }
    
    func loadAds() async {
        self.state = .loading
        do {
            let ads = try await repository.fetchAds()
            self.allAds = ads
            self.filterAds()
            
            if ads.isEmpty {
                self.state = .empty
            } else {
                self.state = .loaded
            }
        } catch {
            self.state = .error(error.localizedDescription)
        }
    }
    
    func selectCategory(_ category: AdType?) {
        self.selectedCategory = category
        filterAds()
    }
    
    private func filterAds() {
        if let category = selectedCategory {
            self.filteredAds = allAds.filter { $0.type == category }
        } else {
            self.filteredAds = allAds
        }
    }
    
    // İletişim Aksiyonu
    func contactAdOwner(info: String?) {
        guard let info = info else { return }
        
        // Eğer email ise mail at, telefon ise ara
        if info.contains("@") {
            if let url = URL(string: "mailto:\(info)") {
                UIApplication.shared.open(url)
            }
        } else {
            let cleanPhone = info.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
            if let url = URL(string: "tel://\(cleanPhone)") {
                UIApplication.shared.open(url)
            }
        }
    }
}


