import SwiftUI

struct NewsDetailView: View {
    let news: News
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // 1. Büyük Görsel
                AsyncImage(url: URL(string: news.imageUrls?.first ?? "")) { image in
                    image
                        .resizable()
                        .aspectRatio(contentMode: .fill)
                } placeholder: {
                    Rectangle()
                        .fill(Color.gray.opacity(0.2))
                        .frame(height: 250)
                        .overlay(ProgressView())
                }
                .frame(height: 250)
                .clipped()
                
                VStack(alignment: .leading, spacing: 16) {
                    // 2. Kategori ve Tarih
                    HStack {
                        Text(news.category.uppercased())
                            .font(.caption)
                            .fontWeight(.bold)
                            .padding(.horizontal, 10)
                            .padding(.vertical, 5)
                            .background(Color.red)
                            .foregroundColor(.white)
                            .cornerRadius(8)
                        
                        Spacer()
                        
                        Text(formatDate(news.publishedAt))
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    
                    // 3. Başlık
                    Text(news.title)
                        .font(.title2) // iOS'te h2 benzeri
                        .fontWeight(.bold)
                        .foregroundColor(.primary)
                    
                    Divider()
                    
                    // 4. İçerik Metni
                    // Not: HTML içeriği varsa burada AttributedText kullanılmalı.
                    // Şimdilik düz metin varsayıyoruz.
                    Text(news.content)
                        .font(.body)
                        .lineSpacing(6) // Okunabilirliği artırır
                        .foregroundColor(Color(.label))
                    
                    Spacer(minLength: 30)
                    
                    // 5. Kaynak Bilgisi
                    HStack {
                        Text("Kaynak:")
                            .fontWeight(.bold)
                        Text(news.source ?? "Kıbrıs Gerçek")
                            .foregroundColor(.blue)
                    }
                    .font(.subheadline)
                    .padding(.top)
                }
                .padding()
            }
        }
        .ignoresSafeArea(edges: .top) // Görselin status bar altına girmesi için
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                ShareLink(item: URL(string: "https://kibrisgercek.com/news/\(news.id)")!) {
                    Image(systemName: "square.and.arrow.up")
                }
            }
        }
    }
    
    private func formatDate(_ date: Date?) -> String {
        guard let date = date else { return "" }
        let formatter = DateFormatter()
        formatter.dateStyle = .long
        formatter.timeStyle = .short
        formatter.locale = Locale(identifier: "tr_TR")
        return formatter.string(from: date)
    }
}

