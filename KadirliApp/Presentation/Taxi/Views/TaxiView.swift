import SwiftUI

struct TaxiView: View {
    @StateObject private var viewModel = TaxiViewModel()
    @State private var showPhoneInput = false
    @State private var selectedTaxi: GuideItem?
    @State private var userPhone = "" // Kullanıcının girdiği numara
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVStack(spacing: 16) {
                    // Başlık Alanı
                    HStack {
                        VStack(alignment: .leading) {
                            Text("En Yakın Taksi")
                                .font(.largeTitle).fontWeight(.black)
                            Text("7/24 Hizmetinizde").foregroundColor(.secondary)
                        }
                        Spacer()
                        Image(systemName: "car.circle.fill")
                            .font(.system(size: 50)).foregroundColor(.yellow)
                    }
                    .padding()
                    
                    // Durum Mesajı
                    if let status = viewModel.requestStatus {
                        Text(status)
                            .padding()
                            .background(Color.blue.opacity(0.1))
                            .foregroundColor(.blue)
                            .cornerRadius(8)
                    }
                    
                    switch viewModel.state {
                    case .loading: ProgressView().padding()
                    case .loaded:
                        if viewModel.taxis.isEmpty {
                            Text("Aktif taksi bulunamadı.").padding()
                        } else {
                            ForEach(viewModel.taxis) { taxi in
                                TaxiRowView(taxi: taxi) {
                                    // Butona basılınca numara sorma ekranını aç
                                    selectedTaxi = taxi
                                    showPhoneInput = true
                                }
                            }
                        }
                    case .empty: Text("Taksi yok.")
                    case .error(let msg): Text(msg)
                    }
                }
                .padding(.bottom, 20)
            }
        }
        .navigationTitle("Taksi Çağır")
        .task { await viewModel.loadTaxis() }
        
        // Numara Girme Popup'ı
        .alert("Taksi Çağır", isPresented: $showPhoneInput) {
            TextField("Telefon Numaranız", text: $userPhone)
                .keyboardType(.numberPad)
            Button("Çağır", role: .none) {
                if let taxi = selectedTaxi {
                    Task { await viewModel.callTaxi(taxi: taxi, userPhone: userPhone) }
                }
            }
            Button("İptal", role: .cancel) {}
        } message: {
            Text("Taksicinin size ulaşabilmesi için numaranızı giriniz.")
        }
    }
}

// SATIR TASARIMI (Burada siyah buton var)
struct TaxiRowView: View {
    let taxi: GuideItem
    let onCall: () -> Void // Tıklama aksiyonu
    
    var body: some View {
        HStack(spacing: 16) {
            // Araba İkonu
            ZStack {
                Circle().fill(Color.yellow.opacity(0.2))
                    .frame(width: 50, height: 50)
                Image(systemName: "car.fill")
                    .foregroundColor(.orange)
            }
            
            VStack(alignment: .leading, spacing: 4) {
                Text(taxi.title)
                    .font(.title3)
                    .fontWeight(.bold)
                
                // Müsaitlik Durumu
                HStack(spacing: 4) {
                    Circle().fill(Color.green).frame(width: 8, height: 8)
                    Text("Müsait")
                        .font(.caption2)
                        .foregroundColor(.green)
                        .fontWeight(.bold)
                }
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .background(Color.green.opacity(0.1))
                .cornerRadius(4)
            }
            
            Spacer()
            
            // İŞTE YENİ BUTON BURASI (SİYAH ÇAĞIR BUTONU)
            Button(action: onCall) {
                Text("ÇAĞIR")
                    .font(.headline)
                    .fontWeight(.bold)
                    .padding(.horizontal, 20)
                    .padding(.vertical, 10)
                    .background(Color.black)
                    .foregroundColor(.yellow)
                    .cornerRadius(8)
            }
        }
        .padding()
        .background(Color.white)
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.05), radius: 5, x: 0, y: 2)
        .padding(.horizontal)
    }
}
