import SwiftUI

struct CampaignDetailView: View {
    let campaign: Campaign
    
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // Görseller
                if let images = campaign.imageUrls, !images.isEmpty {
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
                
                VStack(alignment: .leading, spacing: 16) {
                    Text(campaign.businessName)
                        .font(.headline)
                        .foregroundColor(.blue)
                    
                    Text(campaign.title)
                        .font(.largeTitle)
                        .fontWeight(.black)
                    
                    Divider()
                    
                    if let desc = campaign.description {
                        Text(desc)
                            .font(.body)
                            .foregroundColor(.secondary)
                    }
                    
                    // İNDİRİM KODU BURADA
                    if let code = campaign.discountCode, !code.isEmpty {
                        VStack(spacing: 10) {
                            Text("İNDİRİM KODUNUZ")
                                .font(.caption)
                                .foregroundColor(.gray)
                            Text(code)
                                .font(.system(size: 32, weight: .bold, design: .monospaced))
                                .padding()
                                .background(Color.green.opacity(0.1))
                                .foregroundColor(.green)
                                .cornerRadius(12)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 12)
                                        .stroke(Color.green, style: StrokeStyle(lineWidth: 2, dash: [5]))
                                )
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.top, 30)
                    }
                }
                .padding()
            }
        }
        .ignoresSafeArea(edges: .top)
    }
}
