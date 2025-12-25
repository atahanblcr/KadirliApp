import SwiftUI

struct ProfileCreationView: View {
    @ObservedObject var viewModel: AuthViewModel
    
    var body: some View {
        ScrollView {
            VStack(spacing: 24) {
                
                Text("Sizi Tanıyalım")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .padding(.top, 40)
                
                // 1. İsim Soyisim
                VStack(alignment: .leading) {
                    Text("Adınız Soyadınız")
                        .font(.caption).foregroundColor(.gray)
                    TextField("Örn: Ali Yılmaz", text: $viewModel.fullName)
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(10)
                }
                
                Divider()
                
                // 2. Konum Tipi Seçimi (Mahalle / Köy)
                VStack(alignment: .leading) {
                    Text("Yaşadığınız Yer")
                        .font(.caption).foregroundColor(.gray)
                    
                    Picker("Konum Tipi", selection: $viewModel.selectedLocationType) {
                        Text("Mahalle").tag(0)
                        Text("Köy").tag(1)
                    }
                    .pickerStyle(.segmented)
                    .onChange(of: viewModel.selectedLocationType) { _ in
                        viewModel.selectedLocation = "" // Tip değişince seçimi sıfırla
                    }
                }
                
                // 3. Akıllı Liste (Seçime göre değişir)
                VStack(alignment: .leading) {
                    Text(viewModel.selectedLocationType == 0 ? "Mahalle Seçin" : "Köy Seçin")
                        .font(.caption).foregroundColor(.gray)
                    
                    Picker("Seçiniz", selection: $viewModel.selectedLocation) {
                        Text("Seçiniz...").tag("")
                        
                        // KadirliConstants'tan veriyi çekiyoruz
                        if viewModel.selectedLocationType == 0 {
                            ForEach(KadirliConstants.neighborhoods, id: \.self) { item in
                                Text(item).tag(item)
                            }
                        } else {
                            ForEach(KadirliConstants.villages, id: \.self) { item in
                                Text(item).tag(item)
                            }
                        }
                    }
                    .pickerStyle(.navigationLink) // Yeni sayfa açarak seçim yaptırır (Temiz görünür)
                    .padding()
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .background(Color(.systemGray6))
                    .cornerRadius(10)
                }
                
                Divider()
                
                // 4. İzinler (KVKK & Pazarlama)
                VStack(alignment: .leading, spacing: 16) {
                    Toggle(isOn: $viewModel.isTermsAccepted) {
                        Text("Kullanım Koşulları ve Aydınlatma Metni'ni okudum, onaylıyorum.")
                            .font(.caption)
                    }
                    .toggleStyle(CheckboxToggleStyle()) // Aşağıda tanımladık
                    
                    Toggle(isOn: $viewModel.isMarketingAccepted) {
                        Text("Kampanya ve duyurulardan haberdar olmak istiyorum (Ticari İleti İzni).")
                            .font(.caption)
                            .foregroundColor(.gray)
                    }
                    .toggleStyle(CheckboxToggleStyle())
                }
                
                Spacer(minLength: 30)
                
                // 5. Kaydet Butonu
                Button(action: { Task { await viewModel.completeProfile() } }) {
                    if viewModel.isLoading {
                        ProgressView().tint(.white)
                    } else {
                        Text("Kaydı Tamamla")
                            .fontWeight(.bold)
                            .frame(maxWidth: .infinity)
                    }
                }
                .frame(height: 50)
                .background(viewModel.isTermsAccepted ? Color.red : Color.gray)
                .foregroundColor(.white)
                .cornerRadius(12)
                .disabled(!viewModel.isTermsAccepted || viewModel.isLoading)
                
            }
            .padding()
        }
        .navigationBarBackButtonHidden(true) // Geri dönülmesin, kayıt zorunlu
    }
}

// Özel Checkbox Stili
struct CheckboxToggleStyle: ToggleStyle {
    func makeBody(configuration: Configuration) -> some View {
        HStack(alignment: .top) {
            Image(systemName: configuration.isOn ? "checkmark.square.fill" : "square")
                .foregroundColor(configuration.isOn ? .red : .gray)
                .font(.system(size: 20))
                .onTapGesture { configuration.isOn.toggle() }
            
            configuration.label
                .onTapGesture { configuration.isOn.toggle() }
        }
    }
}
