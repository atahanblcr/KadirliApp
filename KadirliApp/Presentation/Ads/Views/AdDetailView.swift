import SwiftUI
import Kingfisher

struct AdDetailView: View {
    let ad: Ad
    @ObservedObject var viewModel: AdsViewModel
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                VStack(spacing: 0) {
                    
                    // 1. GÖRSEL ALANI (Carousel)
                    if let images = ad.imageUrls, !images.isEmpty {
                        TabView {
                            ForEach(images, id: \.self) { url in
                                KFImage(URL(string: url))
                                    .placeholder {
                                        Rectangle().fill(Color.gray.opacity(0.2))
                                            .overlay(ProgressView())
                                    }
                                    .resizable()
                                    .aspectRatio(contentMode: .fit) // Resmi kesmeden sığdır
                                    .tag(url)
                            }
                        }
                        .tabViewStyle(PageTabViewStyle())
                        .frame(height: 300)
                        .background(Color.black) // Resim kenarları için siyah fon
                    } else {
                        // Resim yoksa
                        ZStack {
                            Color.gray.opacity(0.1)
                            Image(systemName: "photo.on.rectangle")
                                .font(.largeTitle)
                                .foregroundColor(.gray)
                        }
                        .frame(height: 250)
                    }
                    
                    VStack(alignment: .leading, spacing: 20) {
                        
                        // 2. BAŞLIK VE FİYAT
                        VStack(alignment: .leading, spacing: 8) {
                            Text(ad.title)
                                .font(.title2)
                                .fontWeight(.bold)
                                .foregroundColor(.primary)
                                .lineLimit(3)
                            
                            HStack {
                                if let price = ad.price {
                                    Text(price)
                                        .font(.title)
                                        .fontWeight(.heavy)
                                        .foregroundColor(.red) // Fiyat dikkat çeksin
                                }
                                
                                Spacer()
                                
                                // Kategori Etiketi
                                Text(ad.type.displayName)
                                    .font(.caption)
                                    .fontWeight(.bold)
                                    .padding(.horizontal, 10)
                                    .padding(.vertical, 6)
                                    .background(ad.type.color.opacity(0.1))
                                    .foregroundColor(ad.type.color)
                                    .cornerRadius(8)
                            }
                        }
                        
                        Divider()
                        
                        // 3. YASAL UYARI KUTUSU (İstenilen Özellik)
                        HStack(alignment: .top, spacing: 12) {
                            Image(systemName: "exclamationmark.shield.fill")
                                .font(.title2)
                                .foregroundColor(.orange)
                            
                            VStack(alignment: .leading, spacing: 4) {
                                Text("Güvenlik Uyarısı")
                                    .font(.subheadline)
                                    .fontWeight(.bold)
                                    .foregroundColor(.orange)
                                
                                Text("Kadirli Cepte yer sağlayıcıdır. Ürünü görmeden kapora göndermeyiniz.")
                                    .font(.caption)
                                    .foregroundColor(.primary)
                            }
                        }
                        .padding()
                        .background(Color.orange.opacity(0.1))
                        .cornerRadius(12)
                        .overlay(
                            RoundedRectangle(cornerRadius: 12)
                                .stroke(Color.orange.opacity(0.3), lineWidth: 1)
                        )
                        
                        // 4. SATICI BİLGİSİ
                        HStack(spacing: 12) {
                            ZStack {
                                Circle().fill(Color.gray.opacity(0.2))
                                Image(systemName: "person.fill")
                                    .foregroundColor(.gray)
                            }
                            .frame(width: 50, height: 50)
                            
                            VStack(alignment: .leading, spacing: 2) {
                                Text(ad.sellerName ?? "Kadirli Cepte Üyesi")
                                    .font(.headline)
                                
                                // İlan No (ID'nin son 6 hanesi)
                                Text("İlan No: #\(ad.id.uuidString.prefix(6).uppercased())")
                                    .font(.caption2)
                                    .foregroundColor(.secondary)
                            }
                            
                            Spacer()
                            
                            // Aksiyon Butonları
                            if let contact = ad.contactInfo {
                                Button(action: {
                                    viewModel.contactAdOwner(info: contact)
                                }) {
                                    Image(systemName: contact.contains("@") ? "envelope.fill" : "phone.fill")
                                        .font(.title3)
                                        .padding(10)
                                        .background(Color.blue)
                                        .foregroundColor(.white)
                                        .clipShape(Circle())
                                }
                            }
                        }
                        .padding()
                        .background(Color(.systemBackground))
                        .cornerRadius(12)
                        .shadow(color: Color.black.opacity(0.05), radius: 2, x: 0, y: 1)
                        
                        // 5. AÇIKLAMA
                        VStack(alignment: .leading, spacing: 8) {
                            Text("İlan Açıklaması")
                                .font(.headline)
                            
                            Text(ad.description ?? "Açıklama bulunmuyor.")
                                .font(.body)
                                .foregroundColor(.secondary)
                                .fixedSize(horizontal: false, vertical: true) // Metni sarmala
                        }
                        
                        // 6. HARİTA (Varsa Göster)
                        if let lat = ad.latitude, let long = ad.longitude {
                            Divider()
                            
                            VStack(alignment: .leading, spacing: 8) {
                                Text("Konum")
                                    .font(.headline)
                                
                                Button(action: {
                                    openMap(lat: lat, long: long)
                                }) {
                                    HStack {
                                        Image(systemName: "map.fill")
                                        Text("Haritada Göster")
                                        Spacer()
                                        Image(systemName: "chevron.right")
                                    }
                                    .padding()
                                    .background(Color.blue.opacity(0.1))
                                    .foregroundColor(.blue)
                                    .cornerRadius(12)
                                }
                            }
                        }
                        
                        Spacer(minLength: 50)
                    }
                    .padding()
                }
            }
        }
        .navigationTitle("İlan Detayı")
        .navigationBarTitleDisplayMode(.inline)
    }
    
    // Harita Yardımcısı
    private func openMap(lat: Double, long: Double) {
        let urlString = "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m"
        if let url = URL(string: urlString), UIApplication.shared.canOpenURL(url) {
            UIApplication.shared.open(url)
        }
    }
}
