import SwiftUI
import Kingfisher

struct EventCardView: View {
    let event: Event
    
    var body: some View {
        HStack(spacing: 16) {
            // Tarih Kutusu
            VStack(spacing: 4) {
                Text(getDay(from: event.eventDate))
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundColor(.red)
                Text(getMonth(from: event.eventDate))
                    .font(.caption)
                    .fontWeight(.medium)
                    .textCase(.uppercase)
            }
            .frame(width: 60, height: 70)
            .background(Color.red.opacity(0.1))
            .cornerRadius(12)
            
            // Bilgiler
            VStack(alignment: .leading, spacing: 6) {
                Text(event.title)
                    .font(.headline)
                    .lineLimit(2)
                
                HStack {
                    Image(systemName: "clock")
                    Text(getTime(from: event.eventDate))
                    
                    if let location = event.locationName {
                        Text("•")
                        Image(systemName: "mappin.and.ellipse")
                        Text(location)
                            .lineLimit(1)
                    }
                }
                .font(.caption)
                .foregroundColor(.secondary)
            }
            
            Spacer()
            
            // Küçük Görsel (Varsa)
            // Küçük Görsel (Varsa)
            if let imageUrl = event.imageUrl {
                KFImage(URL(string: imageUrl))
                    .placeholder {
                        Color.gray.opacity(0.2)
                    }
                    .resizable()
                    .aspectRatio(contentMode: .fill)
                    .frame(width: 60, height: 60)
                    .cornerRadius(8)
                    .clipped()
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.05), radius: 5, x: 0, y: 2)
    }
    
    // Helpers
    private func getDay(from date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "d"
        return formatter.string(from: date)
    }
    
    private func getMonth(from date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "MMM" // "Oca", "Şub"
        formatter.locale = Locale(identifier: "tr_TR")
        return formatter.string(from: date)
    }
    
    private func getTime(from date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm"
        return formatter.string(from: date)
    }
}

