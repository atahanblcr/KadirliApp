import SwiftUI
import Combine

struct DriverDashboardView: View {
    let taxiId: String
    @StateObject private var viewModel = DriverViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            if viewModel.isLoading {
                ProgressView("İşlemler yapılıyor...")
            } else if viewModel.requests.isEmpty {
                VStack(spacing: 20) {
                    Image(systemName: "car.circle.fill")
                        .font(.system(size: 80))
                        .foregroundColor(.gray.opacity(0.5))
                    Text("Bekleyen aktif çağrı yok.")
                        .font(.title3)
                        .foregroundColor(.secondary)
                    
                    Button("Yenile") {
                        Task { await viewModel.loadRequests(taxiId: taxiId) }
                    }
                }
            } else {
                ScrollView {
                    LazyVStack(spacing: 16) {
                        ForEach(viewModel.requests) { request in
                            RequestCard(request: request, viewModel: viewModel, taxiId: taxiId)
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
            await viewModel.loadRequests(taxiId: taxiId)
        }
    }
}

// MARK: - KART TASARIMI (GÜNCELLENMİŞ)
struct RequestCard: View {
    let request: TaxiRequest
    @ObservedObject var viewModel: DriverViewModel
    let taxiId: String
    
    var isAccepted: Bool {
        return request.status == "accepted"
    }
    
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            // 1. ÜST BİLGİ
            HStack {
                HStack(spacing: 6) {
                    Circle()
                        .fill(isAccepted ? Color.green : Color.orange)
                        .frame(width: 8, height: 8)
                    
                    Text(isAccepted ? "YOLCULUK BAŞLADI" : "YENİ ÇAĞRI")
                        .font(.caption)
                        .fontWeight(.bold)
                        .foregroundColor(isAccepted ? .green : .orange)
                }
                .padding(.vertical, 6)
                .padding(.horizontal, 10)
                .background(isAccepted ? Color.green.opacity(0.1) : Color.orange.opacity(0.1))
                .cornerRadius(8)
                
                Spacer()
                
                Text(TimeHelper.timeAgo(from: request.createdAt))
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            .padding()
            
            Divider()
            
            // 2. MÜŞTERİ BİLGİLERİ
            VStack(alignment: .leading, spacing: 12) {
                // Telefon Numarası
                HStack {
                    Image(systemName: "phone.fill")
                        .foregroundColor(.blue)
                        .font(.title3)
                    
                    Text(request.passengerPhone ?? "Gizli Numara")
                        .font(.title3)
                        .fontWeight(.semibold)
                        .foregroundColor(.primary)
                }
                
                // Konum Bilgisi
                if let _ = request.pickupLatitude {
                    HStack {
                        Image(systemName: "mappin.and.ellipse")
                            .foregroundColor(.red)
                        Text("Müşteri Konumu Paylaşıldı")
                            .font(.body)
                            .foregroundColor(.secondary)
                    }
                }
            }
            .padding()
            
            Divider()
            
            // 3. BUTONLAR (Mantık Burada Değişiyor)
            HStack(spacing: 0) {
                if !isAccepted {
                    // --- DURUM 1: HENÜZ KABUL EDİLMEDİ ---
                    
                    // REDDET BUTONU
                    Button(action: {
                        Task { await viewModel.handleAction(requestId: request.id.uuidString, action: .decline, taxiId: taxiId) }
                    }) {
                        Text("REDDET")
                            .fontWeight(.bold)
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(Color.red.opacity(0.1))
                            .foregroundColor(.red)
                    }
                    
                    Divider().frame(width: 1)
                    
                    // KABUL ET BUTONU
                    Button(action: {
                        Task { await viewModel.handleAction(requestId: request.id.uuidString, action: .accept, taxiId: taxiId) }
                    }) {
                        Text("KABUL ET")
                            .fontWeight(.bold)
                            .frame(maxWidth: .infinity)
                            .padding()
                            .background(Color.green)
                            .foregroundColor(.white)
                    }
                    
                } else {
                    // --- DURUM 2: KABUL EDİLDİ (Araç Meşgul) ---
                    
                    // ARA BUTONU
                    Button(action: {
                        if let phone = request.passengerPhone {
                             // Sadece rakamları al
                            let clean = phone.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
                            if let url = URL(string: "tel://\(clean)") { UIApplication.shared.open(url) }
                        }
                    }) {
                        VStack {
                            Image(systemName: "phone.fill")
                            Text("Ara")
                        }
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.blue.opacity(0.1))
                        .foregroundColor(.blue)
                    }
                    
                    Divider().frame(width: 1)
                    
                    // HARİTA BUTONU
                    Button(action: {
                        if let lat = request.pickupLatitude, let long = request.pickupLongitude {
                            // Apple Maps Yönlendirmesi
                            let url = URL(string: "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m")!
                            UIApplication.shared.open(url)
                        }
                    }) {
                        VStack {
                            Image(systemName: "map.fill")
                            Text("Harita")
                        }
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.blue.opacity(0.1))
                        .foregroundColor(.blue)
                    }
                    
                    Divider().frame(width: 1)
                    
                    // BİTİR BUTONU
                    Button(action: {
                        Task { await viewModel.handleAction(requestId: request.id.uuidString, action: .complete, taxiId: taxiId) }
                    }) {
                        VStack {
                            Image(systemName: "flag.checkered")
                            Text("Bitir")
                        }
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(Color.green)
                        .foregroundColor(.white)
                    }
                }
            }
            .frame(height: 60)
        }
        .background(Color.white)
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.1), radius: 5, x: 0, y: 2)
    }
}

// MARK: - GÜNCELLENMİŞ VIEW MODEL
@MainActor
class DriverViewModel: ObservableObject {
    @Published var requests: [TaxiRequest] = []
    @Published var isLoading = false
    
    private let repository = GuideRepository()
    
    enum TaxiAction {
        case accept
        case decline
        case complete
    }
    
    func loadRequests(taxiId: String) async {
        if requests.isEmpty { isLoading = true }
        do {
            let items = try await repository.fetchTaxiRequests(taxiId: taxiId)
            self.requests = items
        } catch {
            print("Hata: \(error.localizedDescription)")
        }
        isLoading = false
    }
    
    // Tek bir fonksiyonla tüm aksiyonları yönetiyoruz
    func handleAction(requestId: String, action: TaxiAction, taxiId: String) async {
        isLoading = true
        do {
            switch action {
            case .accept:
                // 1. İsteği kabul et
                try await repository.updateRequestStatus(requestId: requestId, status: "accepted")
                // 2. Taksiyi MEŞGUL yap (Müşteriler artık arayamaz)
                try await repository.updateTaxiBusyStatus(taxiId: taxiId, isBusy: true)
                
            case .decline:
                // 1. İsteği reddet/iptal et
                try await repository.updateRequestStatus(requestId: requestId, status: "cancelled")
                // Taksi durumu değişmez (Zaten boştu)
                
            case .complete:
                // 1. İsteği tamamla
                try await repository.updateRequestStatus(requestId: requestId, status: "completed")
                // 2. Taksiyi MÜSAİT yap (Yeni müşteri alabilir)
                try await repository.updateTaxiBusyStatus(taxiId: taxiId, isBusy: false)
            }
            
            // Listeyi yenile
            await loadRequests(taxiId: taxiId)
            
        } catch {
            print("İşlem hatası: \(error)")
        }
        isLoading = false
    }
}
