import Foundation
import Combine

@MainActor
final class AnnouncementsViewModel: ObservableObject {
    @Published var announcements: [Announcement] = []
    @Published var filteredAnnouncements: [Announcement] = []
    @Published var state: ViewState = .loading
    @Published var selectedType: AnnouncementType? = nil // Filtreleme için
    
    private let repository: AnnouncementRepositoryProtocol
    private let userNeighborhood: String? // Kullanıcının mahallesi
    
    init(repository: AnnouncementRepositoryProtocol? = nil) {
        self.repository = repository ?? AnnouncementRepository()
        
        // SessionManager'dan kullanıcının mahallesini almayı deneyebiliriz
        // Şimdilik UserDefaults veya SessionManager üzerinden alındığını varsayıyoruz
        // Örn: self.userNeighborhood = SessionManager.shared.currentUser?.userMetadata?["neighborhood"]
        self.userNeighborhood = nil // V2'de dinamik yapılacak
    }
    
    func loadAnnouncements() async {
        state = .loading
        do {
            let result = try await repository.fetchAnnouncements()
            self.announcements = result
            filterList()
            state = result.isEmpty ? .empty : .loaded
        } catch {
            state = .error(error.localizedDescription)
        }
    }
    
    func filterBy(type: AnnouncementType?) {
        self.selectedType = type
        filterList()
    }
    
    private func filterList() {
        if let type = selectedType {
            self.filteredAnnouncements = announcements.filter { $0.type == type }
        } else {
            self.filteredAnnouncements = announcements
        }
        
        // İPUCU: Burada ileride "Sadece Benim Mahallem" filtresi de eklenebilir.
        // if let neighborhood = userNeighborhood { ... }
    }
}
