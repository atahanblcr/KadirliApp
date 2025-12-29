import SwiftUI
import Combine

struct DriverDashboardView: View {
    let taxiId: String
    @StateObject private var viewModel = DriverViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            if viewModel.isLoading {
                ProgressView("Siparişler Aranıyor...")
            } else if viewModel.requests.isEmpty {
                VStack(spacing: 20) {
                    Image(systemName: "car.circle")
                        .font(.system(size: 60))
                        .foregroundColor(.gray)
                    Text("Şu an bekleyen çağrı yok.")
                        .font(.title3)
                        .foregroundColor(.secondary)
                    
                    Button("Listeyi Yenile") {
                        Task { await viewModel.loadRequests(taxiId: taxiId) }
                    }
                }
            } else {
                ScrollView {
                    LazyVStack(spacing: 16) {
                        ForEach(viewModel.requests) { request in
                            RequestCard(request: request)
                        }
                    }
                    .padding()
                }
                .refreshable {
                    await viewModel.loadRequests(taxiId: taxiId)
                }
            }
        }
        .navigationTitle("Sürücü Paneli")
        .task {
            // Ekran açılınca verileri çek
            await viewModel.loadRequests(taxiId: taxiId)
        }
    }
}

// KART TASARIMI
struct RequestCard: View {
    let request: TaxiRequest
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("YENİ ÇAĞRI")
                    .font(.caption).fontWeight(.bold)
                    .padding(6)
                    .background(Color.green)
                    .foregroundColor(.white)
                    .cornerRadius(4)
                Spacer()
                Text(request.formattedTime)
                    .font(.caption).foregroundColor(.secondary)
            }
            
            Divider()
            
            HStack {
                Image(systemName: "phone.fill")
                    .foregroundColor(.blue)
                // Telefon Numarası
                Text(request.passengerPhone ?? "Numara Gizli")
                    .font(.headline)
            }
            
            HStack(spacing: 12) {
                // Konuma Git Butonu
                if let lat = request.pickupLatitude, let long = request.pickupLongitude {
                    Button(action: {
                        let url = URL(string: "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m")!
                        UIApplication.shared.open(url)
                    }) {
                        Label("Konuma Git", systemImage: "map.fill")
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 10)
                            .background(Color.blue.opacity(0.1))
                            .foregroundColor(.blue)
                            .cornerRadius(8)
                    }
                }
                
                // Ara Butonu
                if let phone = request.passengerPhone {
                    Button(action: {
                        let clean = phone.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
                        if let url = URL(string: "tel://\(clean)") { UIApplication.shared.open(url) }
                    }) {
                        Label("Müşteriyi Ara", systemImage: "phone")
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 10)
                            .background(Color.green.opacity(0.1))
                            .foregroundColor(.green)
                            .cornerRadius(8)
                    }
                }
            }
        }
        .padding()
        .background(Color.white)
        .cornerRadius(12)
        .shadow(radius: 2)
    }
}

// VIEW MODEL (ARTIK GERÇEK)
@MainActor
class DriverViewModel: ObservableObject {
    @Published var requests: [TaxiRequest] = []
    @Published var isLoading = false
    
    private let repository = GuideRepository() // Repository'i kullan
    
    func loadRequests(taxiId: String) async {
        isLoading = true
        do {
            // ARTIK SAHTE VERİ YOK, GERÇEKTEN ÇEKİYORUZ:
            let items = try await repository.fetchTaxiRequests(taxiId: taxiId)
            self.requests = items
        } catch {
            print("Hata: \(error.localizedDescription)")
        }
        isLoading = false
    }
}
