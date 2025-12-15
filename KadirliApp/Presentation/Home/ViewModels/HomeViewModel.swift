import Foundation
import SwiftUI
import Combine

@MainActor
final class HomeViewModel: ObservableObject {
    
    // Navigasyon rotasını tutan path
    @Published var navigationPath = NavigationPath()
    
    // Menü öğeleri
    let menuOptions = HomeMenuOption.allCases
    
    // Ayarlar sayfasına gitmek için tetikleyici
    @Published var showSettings = false
    
    func navigate(to option: HomeMenuOption) {
        navigationPath.append(option)
    }
    
    func openSettings() {
        showSettings = true
    }
}
