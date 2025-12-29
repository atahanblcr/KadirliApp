import Foundation
import SwiftUI
import Combine

@MainActor
final class AdsViewModel: ObservableObject {
    
    @Published var state: ViewState = .loaded // Başlangıçta loaded olsun
    @Published var selectedCategory: AdType? = nil
    
    private var allAds: [Ad] = []
    @Published var filteredAds: [Ad] = []
    
    private let repository: AdsRepositoryProtocol
    
    // IP Alma Servisi (Güvenlik için)
    // Şimdilik sadece repository'yi kullanıyoruz, IP'yi V2'ye bırakabiliriz veya
    // IPHelper.getPublicIP() ile alıp veriye ekleyebiliriz.
    
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
    
    // YENİ: İlan Gönderme İşlemi
    func submitAd(title: String, description: String, price: String, type: AdType, contactInfo: String, sellerName: String, images: [UIImage]) async -> Bool {
        self.state = .loading
        
        do {
            // Repository'e gönder
            try await repository.createAd(
                title: title,
                description: description,
                price: price,
                type: type.rawValue,
                contactInfo: contactInfo,
                sellerName: sellerName,
                images: images,
                lat: nil, // Şimdilik konum seçici yok, V2'de eklenecek
                long: nil
            )
            
            // Başarılı olursa listeyi yenile
            await loadAds()
            return true
            
        } catch {
            self.state = .error("İlan gönderilemedi: \(error.localizedDescription)")
            return false
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
    
    func contactAdOwner(info: String?) {
        guard let info = info else { return }
        if info.contains("@") {
            if let url = URL(string: "mailto:\(info)") { UIApplication.shared.open(url) }
        } else {
            let clean = info.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
            if let url = URL(string: "tel://\(clean)") { UIApplication.shared.open(url) }
        }
    }
}
