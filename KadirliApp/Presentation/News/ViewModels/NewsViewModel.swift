import Foundation
import SwiftUI
import Combine

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
    
    // Pagination (Sayfalama) Değişkenleri
    private var currentPage = 0
    var canLoadMore = true       // Başka sayfa var mı?
    private var isLoadingMore = false // Şu an yükleme yapılıyor mu?
    
    private let repository: NewsRepositoryProtocol
    
    init(repository: NewsRepositoryProtocol? = nil) {
        self.repository = repository ?? NewsRepository()
    }
    
    // 1. İlk Yükleme (Veya Yenileme)
    func loadNews(refresh: Bool = false) async {
        if refresh {
            currentPage = 0
            canLoadMore = true
            newsList.removeAll()
            state = .loading
        }
        
        await fetchNews(page: currentPage)
    }
    
    // 2. Sayfa Sonu Gelince Çağrılır
    func loadMoreNews() async {
        // Eğer zaten yükleniyorsa veya yüklenecek sayfa kalmadıysa dur.
        guard canLoadMore, !isLoadingMore else { return }
        
        await fetchNews(page: currentPage + 1)
    }
    
    // 3. Ortak Veri Çekme Fonksiyonu
    private func fetchNews(page: Int) async {
        isLoadingMore = true
        
        do {
            // Repository'deki yeni fonksiyonu çağırıyoruz
            let newItems = try await repository.fetchLatestNews(page: page)
            
            if newItems.isEmpty {
                // Eğer boş geliyorsa, demek ki veri bitti
                canLoadMore = false
                if newsList.isEmpty { state = .empty }
            } else {
                // Yeni gelenleri mevcut listenin altına ekle
                self.newsList.append(contentsOf: newItems)
                self.currentPage = page
                self.state = .loaded
            }
        } catch {
            // Eğer liste boşsa ve hata aldıysak hata ekranı göster
            // Liste doluysa (altta dönüyorsa) kullanıcıya hata göstermeden sessizce durabiliriz
            if newsList.isEmpty {
                self.state = .error(error.localizedDescription)
            }
        }
        
        isLoadingMore = false
    }
}
