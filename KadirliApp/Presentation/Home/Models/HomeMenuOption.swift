import SwiftUI

enum HomeMenuOption: String, CaseIterable, Identifiable {
    case news, deaths, pharmacy, events, campaigns, places, ads // .places eklendi
    
    var id: String { self.rawValue }
    
    var title: String {
        switch self {
        case .news: return "Haberler"
        case .deaths: return "Vefat İlanları"
        case .pharmacy: return "Nöbetçi Eczane"
        case .events: return "Etkinlikler"
        case .campaigns: return "Kampanyalar"
        case .places: return "Gezilecek Yerler" // Yeni Başlık
        case .ads: return "Seri İlanlar"
        }
    }
    
    var iconName: String {
        switch self {
        case .news: return "newspaper.fill"
        case .deaths: return "heart.slash.fill"
        case .pharmacy: return "cross.case.fill"
        case .events: return "calendar"
        case .campaigns: return "tag.fill"
        case .places: return "map.fill" // Yeni İkon
        case .ads: return "megaphone.fill"
        }
    }
    
    var color: Color {
        switch self {
        case .news: return Color.red
        case .deaths: return Color.black
        case .pharmacy: return Color.green
        case .events: return Color.purple
        case .campaigns: return Color.blue
        case .places: return Color.cyan // Turkuaz/Cyan
        case .ads: return Color.orange
        }
    }
    
    var gradient: LinearGradient {
        return LinearGradient(
            gradient: Gradient(colors: [self.color.opacity(0.8), self.color]),
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
    }
}
