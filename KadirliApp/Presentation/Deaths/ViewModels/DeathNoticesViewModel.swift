import Foundation
import SwiftUI
import Combine // Düzeltme 1: Eklendi

@MainActor
final class DeathNoticesViewModel: ObservableObject {
    
    // MARK: - Properties
    @Published var deaths: [DeathNotice] = []
    @Published var state: ViewState = .loading
    @Published var selectedDate: Date = Date()
    
    // Son 7 günü tutan liste (Filtreleme için)
    let availableDates: [Date]
    
    private let repository: DeathRepositoryProtocol
    
    // MARK: - Init
    // Düzeltme 2: Init hatası giderildi
    init(repository: DeathRepositoryProtocol? = nil) {
        self.repository = repository ?? DeathRepository()
        
        // Son 7 günü oluştur
        var dates: [Date] = []
        for i in 0..<7 {
            if let date = Calendar.current.date(byAdding: .day, value: -i, to: Date()) {
                dates.append(date)
            }
        }
        self.availableDates = dates
    }
    
    // MARK: - Methods
    func loadDeaths() async {
        self.state = .loading
        
        do {
            let result = try await repository.fetchDeaths(for: selectedDate)
            if result.isEmpty {
                self.state = .empty
            } else {
                self.deaths = result
                self.state = .loaded
            }
        } catch {
            self.state = .error(error.localizedDescription)
        }
    }
    
    /// Harita uygulamasını açar
    func openMap(lat: Double?, long: Double?, name: String) {
        guard let lat = lat, let long = long else { return }
        
        // Apple Maps URL Scheme
        let urlString = "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m"
        
        if let url = URL(string: urlString) {
            if UIApplication.shared.canOpenURL(url) {
                UIApplication.shared.open(url)
            }
        }
    }
    
    // Tarihi UI için formatla
    func formatDateForFilter(_ date: Date) -> String {
        if Calendar.current.isDateInToday(date) {
            return "Bugün"
        } else if Calendar.current.isDateInYesterday(date) {
            return "Dün"
        } else {
            let formatter = DateFormatter()
            formatter.dateFormat = "d MMM"
            formatter.locale = Locale(identifier: "tr_TR")
            return formatter.string(from: date)
        }
    }
}
