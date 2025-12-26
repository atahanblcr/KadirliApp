import SwiftUI

// 1. ANA EKRAN (Adım 2.2'deki kısım)
struct ProfileCreationView: View {
    @ObservedObject var viewModel: AuthViewModel
    @State private var showTerms = false // Sözleşme ekranını açmak için
    
    var body: some View {
        ScrollView {
            VStack(spacing: 24) {
                
                Text("Sizi Tanıyalım")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .padding(.top, 40)
                
                // 1. KULLANICI ADI (Ad Soyad yerine bunu istiyoruz)
                VStack(alignment: .leading) {
                    Text("Kullanıcı Adı")
                        .font(.caption).foregroundColor(.gray)
                    
                    TextField("Örn: ÇılgınKadirli80", text: $viewModel.username)
                        .padding()
                        .background(Color(.systemGray6))
                        .cornerRadius(10)
                        .autocapitalization(.none) // Otomatik baş harf büyütmeyi kapat
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
                        viewModel.selectedLocation = ""
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
                    .pickerStyle(.navigationLink)
                    .padding()
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .background(Color(.systemGray6))
                    .cornerRadius(10)
                }
                
                Divider()
                
                // 4. İzinler (Sözleşme linki eklendi)
                VStack(alignment: .leading, spacing: 16) {
                    HStack(alignment: .top) {
                        // Kutu (Checkbox)
                        Image(systemName: viewModel.isTermsAccepted ? "checkmark.square.fill" : "square")
                            .foregroundColor(viewModel.isTermsAccepted ? .red : .gray)
                            .font(.system(size: 24))
                            .onTapGesture { viewModel.isTermsAccepted.toggle() }
                        
                        // Tıklanabilir Yazı
                        VStack(alignment: .leading) {
                            Text("Kullanım Koşulları ve Aydınlatma Metni")
                                .foregroundColor(.blue) // Link olduğu belli olsun diye mavi
                                .underline() // Altı çizili
                                .onTapGesture {
                                    showTerms = true // Tıklayınca pencereyi aç
                                }
                            Text("'ni okudum, onaylıyorum.")
                                .foregroundColor(.primary)
                        }
                        .font(.caption)
                    }
                    
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
        .navigationBarBackButtonHidden(true) // Geri butonu gizli
        // 👇 SÖZLEŞME PENCERESİ BURAYA BAĞLANDI
        .sheet(isPresented: $showTerms) {
            TermsView()
        }
    }
}

// 2. YARDIMCI PENCERE (Adım 2.1'deki kısım - Dosyanın altına ekledik)
struct TermsView: View {
    @Environment(\.dismiss) var dismiss
    
    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 16) {
                    Text("Kullanıcı Sözleşmesi ve Gizlilik Politikası")
                        .font(.title2)
                        .fontWeight(.bold)
                    
                    Text("""
                    1. Taraflar
                    Bu sözleşme, Kadirli Cepte uygulaması ile kullanıcı arasında düzenlenmiştir.
                    
                    2. Veri Gizliliği (KVKK)
                    Kişisel verileriniz (Telefon numarası, mahalle bilgisi vb.) sadece uygulama içi deneyimi iyileştirmek amacıyla işlenmektedir. Üçüncü şahıslarla paylaşılmamaktadır.
                    
                    3. Kullanıcı Sorumlulukları
                    Kullanıcı, belirlediği kullanıcı adının genel ahlak kurallarına uygun olmasından sorumludur.
                    
                    (Buraya ileride daha detaylı hukuki metinler eklenebilir.)
                    """)
                    .font(.body)
                }
                .padding()
            }
            .navigationTitle("Sözleşme")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Kapat") {
                        dismiss()
                    }
                }
            }
        }
    }
}

// 3. YARDIMCI STİL (Checkbox Görünümü)
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
