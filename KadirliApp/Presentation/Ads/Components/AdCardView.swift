import SwiftUI

struct AdCardView: View {
    let ad: Ad
    
    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            // Üst: Kategori ve Fiyat
            HStack {
                Text(ad.type.displayName)
                    .font(.caption2)
                    .fontWeight(.bold)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(ad.type.color.opacity(0.1))
                    .foregroundColor(ad.type.color)
                    .cornerRadius(4)
                
                Spacer()
                
                if let price = ad.price {
                    Text(price)
                        .font(.subheadline)
                        .fontWeight(.semibold)
                        .foregroundColor(.green)
                }
            }
            
            // Başlık
            Text(ad.title)
                .font(.headline)
                .lineLimit(2)
                .foregroundColor(.primary)
            
            // Açıklama (Kısa)
            if let desc = ad.description {
                Text(desc)
                    .font(.caption)
                    .foregroundColor(.secondary)
                    .lineLimit(2)
            }
            
            // Görsel (Varsa)
            if let imageUrl = ad.imageUrls?.first {
                AsyncImage(url: URL(string: imageUrl)) { image in
                    image.resizable()
                        .aspectRatio(4/3, contentMode: .fill) // Düzeltilen Kısım
                } placeholder: {
                    Color.gray.opacity(0.1)
                }
                .frame(height: 200) // Yüksekliği artırdık
                .cornerRadius(8)
                .clipped() // Taşan kısımları kes
                .padding(.top, 4)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.05), radius: 4, x: 0, y: 2)
    }
}
