import SwiftUI

struct DeathCardView: View {
    let death: DeathNotice
    let onMapTap: () -> Void
    
    var body: some View {
        HStack(alignment: .top, spacing: 16) {
            // 1. Fotoğraf Alanı
            if let imageUrl = death.imageUrl, !imageUrl.isEmpty {
                AsyncImage(url: URL(string: imageUrl)) { image in
                    image.resizable().aspectRatio(contentMode: .fill)
                } placeholder: {
                    Color.gray.opacity(0.3)
                }
                .frame(width: 80, height: 100)
                .cornerRadius(8)
                .clipped()
            } else {
                // Fotoğraf yoksa ikon göster
                ZStack {
                    Color.gray.opacity(0.1)
                    Image(systemName: "person.fill")
                        .font(.largeTitle)
                        .foregroundColor(.gray)
                }
                .frame(width: 80, height: 100)
                .cornerRadius(8)
            }
            
            // 2. Bilgiler Alanı
            VStack(alignment: .leading, spacing: 6) {
                // İsim Soyisim
                Text(death.fullName)
                    .font(.headline)
                    .fontWeight(.bold)
                    .foregroundColor(.primary)
                    .lineLimit(1)
                
                Divider()
                
                // Defin Saati ve Tarih Etiketi
                HStack {
                    // Saat
                    if let time = death.burialTime {
                        Text("Defin: \(formatTime(time))")
                            .font(.subheadline)
                            .fontWeight(.semibold)
                            .foregroundColor(.primary)
                    }
                    
                    Spacer()
                    
                    // Tarih Etiketi (Bugün/Yarın)
                    let labelInfo = getDateLabelInfo(dateString: death.deathDate)
                    Text(labelInfo.text)
                        .font(.caption)
                        .fontWeight(.bold)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 4)
                        .background(labelInfo.color)
                        .foregroundColor(.white)
                        .cornerRadius(4)
                }
                
                // Mezarlık Bilgisi
                if let place = death.burialPlace {
                    HStack(spacing: 4) {
                        Image(systemName: "mappin.and.ellipse")
                            .font(.caption)
                            .foregroundColor(.red)
                        Text(place)
                            .font(.caption)
                            .foregroundColor(.secondary)
                            .lineLimit(1)
                    }
                }
                
                // Taziye Evi ve Harita
                if let address = death.condolenceAddress {
                    HStack(alignment: .top) {
                        Text("Taziye Evi: \(address)")
                            .font(.caption)
                            .foregroundColor(.secondary)
                            .lineLimit(2)
                        
                        Spacer()
                        
                        // Harita Butonu (Sadece koordinat varsa)
                        if death.latitude != nil {
                            Button(action: onMapTap) {
                                Image(systemName: "map.fill")
                                    .font(.caption)
                                    .padding(6)
                                    .background(Color.blue.opacity(0.1))
                                    .foregroundColor(.blue)
                                    .clipShape(Circle())
                            }
                        }
                    }
                    .padding(.top, 2)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.05), radius: 4, x: 0, y: 2)
    }
    
    // MARK: - Yardımcı Fonksiyonlar
    
    // Saati düzenler (14:30:00 -> 14:30)
    private func formatTime(_ timeString: String) -> String {
        let parts = timeString.split(separator: ":")
        if parts.count >= 2 {
            return "\(parts[0]):\(parts[1])"
        }
        return timeString
    }
    
    // Tarihe göre etiket rengi ve metni belirler
    private func getDateLabelInfo(dateString: String) -> (text: String, color: Color) {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyy-MM-dd"
        formatter.locale = Locale(identifier: "tr_TR")
        
        guard let date = formatter.date(from: dateString) else { return ("", .clear) }
        
        let calendar = Calendar.current
        
        if calendar.isDateInToday(date) {
            return ("BUGÜN", .red)
        } else if calendar.isDateInTomorrow(date) {
            return ("YARIN", .orange) // Yarın ise Turuncu/Kırmızı dikkat çekici
        } else if calendar.isDateInYesterday(date) {
            return ("DÜN", .gray)
        } else {
            // Daha eski tarihler için gün farkı
            let components = calendar.dateComponents([.day], from: date, to: Date())
            if let days = components.day, days > 0 {
                return ("\(days) GÜN ÖNCE", .gray)
            }
            return ("", .clear)
        }
    }
}
