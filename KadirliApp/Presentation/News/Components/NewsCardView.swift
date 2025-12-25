import SwiftUI
import Kingfisher

struct NewsCardView: View {
    let news: News
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // 1. Görsel Alanı (DÜZELTİLEN KISIM)
            KFImage(URL(string: news.imageUrls?.first ?? ""))
                .placeholder {
                    Rectangle()
                        .fill(Color.gray.opacity(0.2))
                        .overlay(ProgressView())
                }
                .onFailureImage(UIImage(systemName: "photo"))
                .resizable()
                .aspectRatio(contentMode: .fill)
                .frame(height: 200)
                .clipped()
            
            // 2. İçerik Alanı
            VStack(alignment: .leading, spacing: 10) {
                // Kategori Badge
                Text(news.category.uppercased())
                    .font(.caption)
                    .fontWeight(.bold)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(Color.red.opacity(0.1))
                    .foregroundColor(.red)
                    .cornerRadius(4)
                
                // Başlık
                Text(news.title)
                    .font(.headline)
                    .lineLimit(2)
                    .foregroundColor(.primary)
                
                // Alt Bilgi (Kaynak ve Zaman)
                HStack {
                    Image(systemName: "clock")
                        .font(.caption2)
                    Text(timeAgoDisplay(date: news.publishedAt ?? Date()))
                        .font(.caption)
                    
                    Spacer()
                    
                    Text(news.source ?? "Kıbrıs Gerçek")
                        .font(.caption)
                        .fontWeight(.semibold)
                        .foregroundColor(.gray)
                }
                .foregroundColor(.secondary)
            }
            .padding()
        }
        .background(Color(.systemBackground))
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.1), radius: 5, x: 0, y: 2)
    }
    
    // Yardımcı: Zaman formatlayıcı
    private func timeAgoDisplay(date: Date) -> String {
        let formatter = RelativeDateTimeFormatter()
        formatter.unitsStyle = .full
        formatter.locale = Locale(identifier: "tr_TR")
        return formatter.localizedString(for: date, relativeTo: Date())
    }
}

// Skeleton View (Yükleniyor hali)
struct NewsCardSkeleton: View {
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Rectangle()
                .fill(Color.gray.opacity(0.3))
                .frame(height: 200)
            
            VStack(alignment: .leading, spacing: 10) {
                Rectangle()
                    .fill(Color.gray.opacity(0.3))
                    .frame(width: 80, height: 20)
                    .cornerRadius(4)
                
                Rectangle()
                    .fill(Color.gray.opacity(0.3))
                    .frame(height: 20)
                    .cornerRadius(4)
                
                Rectangle()
                    .fill(Color.gray.opacity(0.3))
                    .frame(width: 150, height: 20)
                    .cornerRadius(4)
            }
            .padding()
        }
        .background(Color.white)
        .cornerRadius(16)
        .shimmer()
    }
}
