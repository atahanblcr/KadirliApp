import SwiftUI

struct AdDetailView: View {
    let ad: Ad
    @ObservedObject var viewModel: AdsViewModel // Aksiyon için
    
    var body: some View {
        ScrollView {
            HStack {
                // İlan No (ID'nin son 6 hanesi)
                Text("İlan No: #\(ad.id.uuidString.prefix(6).uppercased())")
                    .font(.caption)
                    .fontWeight(.bold)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(Color.gray.opacity(0.1))
                    .foregroundColor(.secondary)
                    .cornerRadius(4)
                
                Spacer()
                
                // Tarih (Varsa)
                // Text(formatDate(ad.createdAt)) ...
            }
            VStack(alignment: .leading, spacing: 16) {
                
                // Görseller (Carousel)
                if let images = ad.imageUrls, !images.isEmpty {
                    TabView {
                        ForEach(images, id: \.self) { url in
                            AsyncImage(url: URL(string: url)) { image in
                                image.resizable().aspectRatio(contentMode: .fill)
                            } placeholder: {
                                ProgressView()
                            }
                        }
                    }
                    .frame(height: 250)
                    .tabViewStyle(PageTabViewStyle())
                }
                
                VStack(alignment: .leading, spacing: 16) {
                    // Başlık ve Fiyat
                    HStack(alignment: .top) {
                        Text(ad.title)
                            .font(.title2)
                            .fontWeight(.bold)
                        
                        Spacer()
                        
                        if let price = ad.price {
                            Text(price)
                                .font(.title3)
                                .fontWeight(.bold)
                                .foregroundColor(.green)
                        }
                    }
                    
                    // Kategori Badge
                    Text(ad.type.displayName)
                        .font(.caption)
                        .fontWeight(.bold)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 5)
                        .background(ad.type.color.opacity(0.1))
                        .foregroundColor(ad.type.color)
                        .cornerRadius(8)
                    
                    Divider()
                    
                    // Açıklama
                    Text("İlan Detayı")
                        .font(.headline)
                    
                    Text(ad.description ?? "Açıklama bulunmuyor.")
                        .font(.body)
                        .foregroundColor(.secondary)
                    
                    Spacer(minLength: 30)
                    
                    // İletişim Butonu
                    if let contact = ad.contactInfo {
                        Button(action: {
                            viewModel.contactAdOwner(info: contact)
                        }) {
                            HStack {
                                Image(systemName: contact.contains("@") ? "envelope.fill" : "phone.fill")
                                Text("İletişime Geç")
                            }
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(Color.blue)
                            .foregroundColor(.white)
                            .cornerRadius(12)
                        }
                    }
                }
                .padding()
            }
        }
        .navigationBarTitleDisplayMode(.inline)
    }
}
