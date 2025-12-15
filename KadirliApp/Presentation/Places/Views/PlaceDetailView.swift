import SwiftUI

struct PlaceDetailView: View {
    let place: Place
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // Galeri
                if let images = place.imageUrls, !images.isEmpty {
                    TabView {
                        ForEach(images, id: \.self) { url in
                            AsyncImage(url: URL(string: url)) { img in
                                img.resizable().aspectRatio(contentMode: .fill)
                            } placeholder: { ProgressView() }
                        }
                    }
                    .frame(height: 300)
                    .tabViewStyle(PageTabViewStyle())
                }
                
                VStack(alignment: .leading, spacing: 20) {
                    Text(place.title)
                        .font(.largeTitle)
                        .fontWeight(.bold)
                    
                    if let desc = place.description {
                        Text(desc)
                            .font(.body)
                            .lineSpacing(6)
                            .foregroundColor(.secondary)
                    }
                    
                    if let lat = place.latitude, let long = place.longitude {
                        Button(action: {
                            if let url = URL(string: "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m") {
                                UIApplication.shared.open(url)
                            }
                        }) {
                            Label("Yol Tarifi Al", systemImage: "map.fill")
                                .frame(maxWidth: .infinity)
                                .padding()
                                .background(Color.cyan)
                                .foregroundColor(.white)
                                .cornerRadius(12)
                        }
                        .padding(.top)
                    }
                }
                .padding()
            }
        }
        .ignoresSafeArea(edges: .top)
    }
}

