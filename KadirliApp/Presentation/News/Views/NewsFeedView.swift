import SwiftUI

struct NewsFeedView: View {
    @StateObject private var viewModel = NewsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                // ...
                LazyVStack(spacing: 20) {
                    switch viewModel.state {
                    case .loading:
                        // Skeleton Loading
                        ForEach(0..<5, id: \.self) { _ in
                            NewsCardSkeleton().padding(.horizontal)
                        }
                        
                    case .loaded:
                        // MEVCUT HABERLER
                        ForEach(viewModel.newsList) { news in
                            NavigationLink(destination: NewsDetailView(news: news)) {
                                NewsCardView(news: news)
                            }
                            .buttonStyle(PlainButtonStyle())
                            .padding(.horizontal)
                            // SİHİRLİ DOKUNUŞ BURADA ✨
                            .onAppear {
                                // Eğer ekranda görünen bu haber, listedeki son haber ise...
                                if news.id == viewModel.newsList.last?.id {
                                    Task { await viewModel.loadMoreNews() }
                                }
                            }
                        }
                        
                        // Altta dönen yükleme çubuğu (Veri yükleniyorsa)
                        if viewModel.canLoadMore {
                            ProgressView()
                                .padding()
                        }
                        
                    case .empty:
                        // ... (Eski kodlar aynı) ...
                        VStack(spacing: 20) {
                            Image(systemName: "newspaper")
                            // ...
                        }
                        
                    case .error(let message):
                        // ... (Eski kodlar aynı) ...
                        VStack {
                            Text("Hata Oluştu")
                            // ...
                        }
                    }
                }
                // ...
                .refreshable {
                    // Aşağı çekince listeyi sıfırla ve baştan çek
                    await viewModel.loadNews(refresh: true)
                }
                // ...
                .task {
                    if viewModel.newsList.isEmpty {
                        // İlk açılış (refresh: false varsayılan)
                        await viewModel.loadNews()
                    }
                }
            }
        }
    }
}
