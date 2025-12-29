import SwiftUI
import Kingfisher

struct CampaignsView: View {
    @StateObject private var viewModel = CampaignsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVStack(spacing: 20) {
                    switch viewModel.state {
                    case .loading:
                        ProgressView("Kampanyalar Yükleniyor...")
                            .padding(.top, 50)
                    case .empty:
                        Text("Aktif kampanya bulunmuyor.")
                            .padding(.top, 50)
                            .foregroundColor(.secondary)
                    case .error(let msg):
                        Text("Hata: \(msg)").foregroundColor(.red)
                    case .loaded:
                        // CampaignsView.swift içinde:
                        ForEach(viewModel.campaigns) { campaign in
                            NavigationLink(destination: CampaignDetailView(campaign: campaign)) {
                                CampaignCard(campaign: campaign)
                            }
                            .buttonStyle(PlainButtonStyle())
                        }
                    }
                }
                .padding()
            }
        }
        .navigationTitle("Kampanyalar")
        .task { await viewModel.loadCampaigns() }
    }
}

struct CampaignCard: View {
    let campaign: Campaign
    @State private var showCode = false
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // Görseller (Carousel veya Tek)
            if let images = campaign.imageUrls, !images.isEmpty {
                if images.count > 1 {
                    TabView {
                        ForEach(images, id: \.self) { url in
                            KFImage(URL(string: url))
                                .resizable()
                                .placeholder { Color.gray.opacity(0.2) }
                                .aspectRatio(contentMode: .fill)
                        }
                    }
                    .frame(height: 200)
                    .tabViewStyle(PageTabViewStyle())
                } else {
                    AsyncImage(url: URL(string: images[0])) { img in
                        img.resizable().aspectRatio(contentMode: .fill)
                    } placeholder: { Color.gray.opacity(0.2) }
                    .frame(height: 200)
                    .clipped()
                }
            }
            
            VStack(alignment: .leading, spacing: 12) {
                // İşletme Adı
                Text(campaign.businessName)
                    .font(.caption)
                    .fontWeight(.bold)
                    .padding(6)
                    .background(Color.blue.opacity(0.1))
                    .foregroundColor(.blue)
                    .cornerRadius(4)
                
                // Başlık
                Text(campaign.title)
                    .font(.title3)
                    .fontWeight(.bold)
                
                // Açıklama
                if let desc = campaign.description {
                    Text(desc)
                        .font(.body)
                        .foregroundColor(.secondary)
                }
                
                // İndirim Kodu Alanı
                if let code = campaign.discountCode, !code.isEmpty {
                    Divider()
                    if showCode {
                        HStack {
                            Spacer()
                            Text(code)
                                .font(.headline)
                                .fontWeight(.black)
                                .foregroundColor(.green)
                                .padding()
                                .background(Color.green.opacity(0.1))
                                .cornerRadius(8)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 8)
                                        .stroke(Color.green, style: StrokeStyle(lineWidth: 1, dash: [5]))
                                )
                            Spacer()
                        }
                    } else {
                        Button(action: { withAnimation { showCode = true } }) {
                            Text("İndirim Kodunu Göster")
                                .fontWeight(.bold)
                                .frame(maxWidth: .infinity)
                                .padding()
                                .background(Color.blue)
                                .foregroundColor(.white)
                                .cornerRadius(10)
                        }
                    }
                }
            }
            .padding()
        }
        .background(Color.white)
        .cornerRadius(16)
        .shadow(radius: 2)
    }
}
