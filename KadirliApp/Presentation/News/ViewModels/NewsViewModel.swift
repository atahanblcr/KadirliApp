import Foundation
import SwiftUI
import Combine // <-- 1. Düzeltme: Combine eklendi

enum ViewState {
    case loading
    case loaded
    case error(String)
    case empty
}

@MainActor
final class NewsViewModel: ObservableObject {
    
    @Published var newsList: [News] = []
    @Published var state: ViewState = .loading
    
    private let repository: NewsRepositoryProtocol
    
    // Dependency Injection
    // 2. Düzeltme: Varsayılan değer parantez içinden kaldırılıp içeri alındı.
    init(repository: NewsRepositoryProtocol? = nil) {
        self.repository = repository ?? NewsRepository()
    }
    
    func loadNews() async {
        self.state = .loading
        
        do {
            let news = try await repository.fetchLatestNews()
            if news.isEmpty {
                self.state = .empty
            } else {
                self.newsList = news
                self.state = .loaded
            }
        } catch {
            self.state = .error(error.localizedDescription)
        }
    }
}
