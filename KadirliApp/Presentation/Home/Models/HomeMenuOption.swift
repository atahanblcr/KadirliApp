import SwiftUI

enum HomeMenuOption: String, CaseIterable, Identifiable {
    case taxi
    case announcements
    case guide, deaths, pharmacy, events, campaigns, places, ads, transport
    
    var id: String { self.rawValue }
    
    var title: String {
        switch self {
        case .taxi: return "Taksi Çağır"
        case .announcements: return "Duyurular"
        case .guide: return "Altın Rehber"
        case .deaths: return "Vefat İlanları"
        case .pharmacy: return "Nöbetçi Eczane"
        case .events: return "Etkinlikler"
        case .campaigns: return "Kampanyalar"
        case .places: return "Gezilecek Yerler"
        case .ads: return "Sıfır & 2.El Pazarı"
        case .transport: return "Ulaşım"
        }
    }
    
    var iconName: String {
        switch self {
        case .taxi: return "car.circle.fill"
        case .announcements: return "megaphone.fill"
        case .guide: return "book.fill"
        case .deaths: return "heart.slash.fill"
        case .pharmacy: return "cross.case.fill"
        case .events: return "calendar"
        case .campaigns: return "tag.fill"
        case .places: return "map.fill"
        case .ads: return "megaphone.fill"
        case .transport: return "bus.fill"
        }
    }
    
    var color: Color {
        switch self {
        case .taxi: return Color.yellow
        case .announcements: return Color.purple
        case .guide: return Color.red
        case .deaths: return Color.black
        case .pharmacy: return Color.green
        case .events: return Color.purple
        case .campaigns: return Color.blue
        case .places: return Color.cyan
        case .ads: return Color.orange
        case .transport: return Color.indigo
        }
    }
    
    var gradient: LinearGradient {
        // Taksi için özel sarı-siyah kontrastı veya sarı-turuncu gradyanı
        if self == .taxi {
            return LinearGradient(
                gradient: Gradient(colors: [Color.yellow, Color.orange]),
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
        }
        
        return LinearGradient(
            gradient: Gradient(colors: [self.color.opacity(0.8), self.color]),
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }
}
