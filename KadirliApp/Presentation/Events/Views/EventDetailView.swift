import SwiftUI

struct EventDetailView: View {
    let event: Event
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // Büyük Görsel
                AsyncImage(url: URL(string: event.imageUrl ?? "")) { img in
                    img.resizable().aspectRatio(contentMode: .fill)
                } placeholder: { Rectangle().fill(Color.gray.opacity(0.2)) }
                .frame(height: 250)
                .clipped()
                
                VStack(alignment: .leading, spacing: 20) {
                    // Başlık
                    Text(event.title)
                        .font(.title)
                        .fontWeight(.bold)
                    
                    // Bilgiler
                    HStack(spacing: 20) {
                        Label(formatDate(event.eventDate), systemImage: "calendar")
                        Label(formatTime(event.eventDate), systemImage: "clock")
                    }
                    .foregroundColor(.secondary)
                    
                    if let location = event.locationName {
                        Label(location, systemImage: "mappin.and.ellipse")
                            .font(.headline)
                            .foregroundColor(.blue)
                    }
                    
                    Divider()
                    
                    // Açıklama
                    if let desc = event.description {
                        Text("Etkinlik Detayı")
                            .font(.headline)
                        Text(desc)
                            .foregroundColor(.secondary)
                    }
                    
                    // Yol Tarifi Butonu
                    if let lat = event.latitude, let long = event.longitude {
                        Button(action: { openMap(lat: lat, long: long) }) {
                            Label("Yol Tarifi Al", systemImage: "map.fill")
                                .frame(maxWidth: .infinity)
                                .padding()
                                .background(Color.blue)
                                .foregroundColor(.white)
                                .cornerRadius(12)
                        }
                        .padding(.top)
                    }
                }
                .padding()
            }
        }
        .navigationBarTitleDisplayMode(.inline)
    }
    
    // Helpers
    func formatDate(_ date: Date) -> String {
        let f = DateFormatter(); f.dateFormat = "d MMMM yyyy"; f.locale = Locale(identifier: "tr_TR")
        return f.string(from: date)
    }
    func formatTime(_ date: Date) -> String {
        let f = DateFormatter(); f.dateFormat = "HH:mm"
        return f.string(from: date)
    }
    func openMap(lat: Double, long: Double) {
        if let url = URL(string: "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m") {
            UIApplication.shared.open(url)
        }
    }
}

