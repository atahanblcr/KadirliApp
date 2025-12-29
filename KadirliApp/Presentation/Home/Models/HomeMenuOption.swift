import SwiftUI

enum HomeMenuOption: String, CaseIterable, Identifiable {
    case taxi // 🚖 YENİ: En başa veya uygun bir yere ekle
    case guide, deaths, pharmacy, events, campaigns, places, ads
    
    var id: String { self.rawValue }
    
    var title: String {
        switch self {
        case .taxi: return "Taksi Çağır" // 🚖 YENİ
        case .guide: return "Altın Rehber"
        case .deaths: return "Vefat İlanları"
        case .pharmacy: return "Nöbetçi Eczane"
        case .events: return "Etkinlikler"
        case .campaigns: return "Kampanyalar"
        case .places: return "Gezilecek Yerler"
        case .ads: return "Sıfır & 2.El Pazarı"
        }
    }
    
    var iconName: String {
        switch self {
        case .taxi: return "car.circle.fill" // 🚖 YENİ (veya 'car.fill')
        case .guide: return "book.fill"
        case .deaths: return "heart.slash.fill"
        case .pharmacy: return "cross.case.fill"
        case .events: return "calendar"
        case .campaigns: return "tag.fill"
        case .places: return "map.fill"
        case .ads: return "megaphone.fill"
        }
    }
    
    var color: Color {
        switch self {
        case .taxi: return Color.yellow // 🚖 YENİ: Sarı renk
        case .guide: return Color.red
        case .deaths: return Color.black
        case .pharmacy: return Color.green
        case .events: return Color.purple
        case .campaigns: return Color.blue
        case .places: return Color.cyan
        case .ads: return Color.orange
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
