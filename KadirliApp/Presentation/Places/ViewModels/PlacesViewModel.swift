import Foundation
import Combine

@MainActor
final class PlacesViewModel: ObservableObject {
    @Published var places: [Place] = []
    @Published var state: ViewState = .loading
    
    // Sıralama Modu: false = Varsayılan, true = Yakından Uzağa
    @Published var sortNearest: Bool = false {
        didSet { sortList() }
    }
    
    private let repository: PlaceRepositoryProtocol
    
    init(repository: PlaceRepositoryProtocol? = nil) {
        self.repository = repository ?? PlaceRepository()
    }
    
    func loadPlaces() async {
        state = .loading
        do {
            let result = try await repository.fetchPlaces()
            self.places = result
            // İlk açılışta da sıralama durumuna göre dizelim
            sortList()
            state = .loaded
        } catch {
            state = .error(error.localizedDescription)
        }
    }
    
    private func sortList() {
        if sortNearest {
            // Küçükten büyüğe (0 km -> 15 km)
            places.sort { ($0.distanceKm ?? 999) < ($1.distanceKm ?? 999) }
        } else {
            // Varsayılan (Veritabanından geldiği gibi veya isme göre)
            places.sort { $0.title < $1.title }
        }
    }
}
