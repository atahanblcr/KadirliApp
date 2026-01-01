import SwiftUI
import Kingfisher

struct AnnouncementDetailView: View {
    let announcement: Announcement
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // 1. Görsel (Varsa)
                if let imageUrl = announcement.imageUrl {
                    KFImage(URL(string: imageUrl))
                        .resizable()
                        .placeholder { Color.gray.opacity(0.2) }
                        .aspectRatio(contentMode: .fill)
                        .frame(height: 250)
                        .clipped()
                }
                
                VStack(alignment: .leading, spacing: 16) {
                    // 2. Tip ve Tarih
                    HStack {
                        Label(announcement.type.title, systemImage: announcement.type.iconName)
                            .font(.subheadline)
                            .fontWeight(.bold)
                            .foregroundColor(announcement.type.color)
                        
                        Spacer()
                        
                        Text(formatDate(announcement.createdAt))
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                    
                    Divider()
                    
                    // 3. Başlık ve Kurum
                    VStack(alignment: .leading, spacing: 6) {
                        Text(announcement.title)
                            .font(.title2)
                            .fontWeight(.bold)
                        
                        if let institution = announcement.institutionName {
                            Text(institution)
                                .font(.headline)
                                .foregroundColor(.secondary)
                        }
                    }
                    
                    // 4. Mahalle Bilgisi
                    if let targets = announcement.targetNeighborhoods, !targets.isEmpty {
                        VStack(alignment: .leading) {
                            Text("Etkilenen Bölgeler:")
                                .font(.caption)
                                .fontWeight(.bold)
                            
                            FlowLayout(items: targets) // Basit bir text wrap de yapılabilir
                        }
                        .padding()
                        .background(Color.red.opacity(0.1))
                        .cornerRadius(8)
                    }
                    
                    // 5. Açıklama Metni
                    Text(announcement.description)
                        .font(.body)
                        .padding(.top, 8)
                    
                    // 6. Dosya Eki (PDF vb.)
                    if let fileUrl = announcement.fileUrl, let url = URL(string: fileUrl) {
                        Link(destination: url) {
                            HStack {
                                Image(systemName: "doc.fill")
                                Text("Ekli Dosyayı/Dökümanı Aç")
                                    .fontWeight(.bold)
                            }
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(Color.blue)
                            .foregroundColor(.white)
                            .cornerRadius(12)
                        }
                        .padding(.top, 20)
                    }
                }
                .padding()
            }
        }
        .navigationTitle("Duyuru Detayı")
        .navigationBarTitleDisplayMode(.inline)
    }
    
    func formatDate(_ date: Date) -> String {
        let f = DateFormatter()
        f.dateFormat = "d MMMM yyyy HH:mm"
        f.locale = Locale(identifier: "tr_TR")
        return f.string(from: date)
    }
}

// Yardımcı View: Mahalle listesi için basit görünüm
struct FlowLayout: View {
    let items: [String]
    var body: some View {
        Text(items.joined(separator: ", "))
            .font(.caption)
            .foregroundColor(.primary)
    }
}
