import SwiftUI

struct NewsFeedView: View {
    @StateObject private var viewModel = NewsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVStack(spacing: 20) {
                    switch viewModel.state {
                    case .loading:
                        // Skeleton Loading: 5 tane boş kart göster
                        ForEach(0..<5, id: \.self) { _ in
                            NewsCardSkeleton()
                                .padding(.horizontal)
                        }
                        
                    case .loaded:
                        ForEach(viewModel.newsList) { news in
                            NavigationLink(destination: NewsDetailView(news: news)) {
                                NewsCardView(news: news)
                            }
                            .buttonStyle(PlainButtonStyle()) // NavigationLink mavi rengini kaldırır
                            .padding(.horizontal)
                        }
                        
                    case .empty:
                        VStack(spacing: 20) {
                            Image(systemName: "newspaper")
                                .font(.system(size: 60))
                                .foregroundColor(.gray)
                            Text("Henüz haber bulunmuyor.")
                                .font(.headline)
                                .foregroundColor(.gray)
                        }
                        .padding(.top, 100)
                        
                    case .error(let message):
                        VStack {
                            Image(systemName: "exclamationmark.triangle")
                                .font(.largeTitle)
                                .foregroundColor(.red)
                            Text("Hata Oluştu")
                                .font(.headline)
                            Text(message)
                                .font(.caption)
                                .multilineTextAlignment(.center)
                                .padding()
                            Button("Tekrar Dene") {
                                Task { await viewModel.loadNews() }
                            }
                            .buttonStyle(.borderedProminent)
                        }
                        .padding(.top, 50)
                    }
                }
                .padding(.vertical)
            }
            .refreshable {
                await viewModel.loadNews()
            }
        }
        .navigationTitle("Haberler")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            // View ilk açıldığında veriyi çek
            if viewModel.newsList.isEmpty {
                await viewModel.loadNews()
            }
        }
    }
}
