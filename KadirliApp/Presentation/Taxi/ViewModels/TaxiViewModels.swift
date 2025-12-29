import Foundation
import SwiftUI
import Combine
import CoreLocation

@MainActor
final class TaxiViewModel: ObservableObject {
    @Published var taxis: [GuideItem] = []
    @Published var state: ViewState = .loading
    
    // YENİ EKLENENLER (View'ın aradığı kısımlar)
    @Published var requestStatus: String?
    @Published var userLocation: CLLocation?
    
    private let repository: GuideRepositoryProtocol
    private let locationManager = LocationManager() // Birazdan 2. adımda bunu oluşturacağız
    private var cancellables = Set<AnyCancellable>()
    
    init(repository: GuideRepositoryProtocol? = nil) {
        self.repository = repository ?? GuideRepository()
        
        // Konum güncellemelerini dinle
        locationManager.$location
            .sink { [weak self] loc in
                self?.userLocation = loc
            }
            .store(in: &cancellables)
    }
    
    func loadTaxis() async {
        state = .loading
        // Konum izni iste
        locationManager.requestLocation()
        
        do {
            let categories = try await repository.fetchCategories()
            
            // "Taksi" kelimesi geçen kategoriyi bul
            if let taxiCategory = categories.first(where: { $0.title.contains("Taksi") }) {
                let items = try await repository.fetchItems(categoryId: taxiCategory.id.uuidString)
                self.taxis = items
                state = .loaded
            } else {
                state = .empty
            }
        } catch {
            state = .error(error.localizedDescription)
        }
    }
    
    // YENİ: Taksi Çağırma Fonksiyonu (Güncellendi)
        func callTaxi(taxi: GuideItem, userPhone: String) async {
            
            // Validasyon: Numara girilmiş mi?
            if userPhone.count < 10 {
                requestStatus = "Lütfen geçerli bir numara girin."
                return
            }
            
            requestStatus = "Çağrı gönderiliyor..."
            
            // KONUM HİLESİ:
            // Eğer konum varsa gerçek konumu al, yoksa (Simülatör hatası veya izin yoksa) 0.0 gönder.
            // Böylece uygulama asla takılmaz.
            let latitude = userLocation?.coordinate.latitude ?? 0.0
            let longitude = userLocation?.coordinate.longitude ?? 0.0
            
            do {
                // Repository'deki fonksiyonu çağır
                try await repository.sendTaxiRequest(
                    taxiId: taxi.id.uuidString,
                    phone: userPhone,
                    lat: latitude,   // 0.0 gitse bile sorun yok, numara gidiyor.
                    long: longitude
                )
                requestStatus = "✅ Çağrı gönderildi! Taksiciye numaranız iletildi."
                
                // Kullanıcıya mesajı okuması için 2 saniye fırsat verip listeyi yenileyelim
                try? await Task.sleep(nanoseconds: 2 * 1_000_000_000)
                requestStatus = nil // Mesajı temizle
                
            } catch {
                requestStatus = "❌ Hata: \(error.localizedDescription)"
            }
        }
}
