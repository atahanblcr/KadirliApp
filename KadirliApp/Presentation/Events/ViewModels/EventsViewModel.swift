import Foundation
import SwiftUI
import Combine // Düzeltme 1

enum EventFilter: String, CaseIterable {
    case today = "Bugün"
    case upcoming = "Yaklaşan"
}

@MainActor
final class EventsViewModel: ObservableObject {
    
    @Published var state: ViewState = .loading
    @Published var selectedFilter: EventFilter = .today
    
    // Ham veri
    private var allEvents: [Event] = []
    
    // Filtrelenmiş listeler
    @Published var todayEvents: [Event] = []
    @Published var upcomingEvents: [Event] = []
    
    private let repository: EventsRepositoryProtocol
    
    // Düzeltme 2: Init hatası giderildi
    init(repository: EventsRepositoryProtocol? = nil) {
        self.repository = repository ?? EventsRepository()
    }
    
    func loadEvents() async {
        self.state = .loading
        do {
            let events = try await repository.fetchEvents()
            self.allEvents = events
            self.filterEvents()
            
            if events.isEmpty {
                self.state = .empty
            } else {
                self.state = .loaded
            }
        } catch {
            self.state = .error(error.localizedDescription)
        }
    }
    
    private func filterEvents() {
        let calendar = Calendar.current
        
        // Bugünün etkinlikleri
        self.todayEvents = allEvents.filter { event in
            calendar.isDateInToday(event.eventDate)
        }
        
        // Gelecek etkinlikler (Yarından itibaren)
        self.upcomingEvents = allEvents.filter { event in
            // Bugün değilse ve tarih bugünden büyükse
            !calendar.isDateInToday(event.eventDate) && event.eventDate > Date()
        }
    }
    
    // UI'da gösterilecek liste
    var currentDisplayList: [Event] {
        switch selectedFilter {
        case .today: return todayEvents
        case .upcoming: return upcomingEvents
        }
    }
}
