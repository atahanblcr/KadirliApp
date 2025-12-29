import SwiftUI
import PhotosUI // Fotoğraf seçimi için gerekli

struct AddAdView: View {
    @Environment(\.dismiss) var dismiss
    @StateObject var viewModel: AdsViewModel // Logic'i buradan yöneteceğiz
    
    // Form Alanları
    @State private var title = ""
    @State private var price = ""
    @State private var description = ""
    @State private var selectedCategory: AdType = .secondHand
    @State private var sellerName = ""
    @State private var contactInfo = ""
    
    // Fotoğraf Seçimi
    @State private var selectedItems: [PhotosPickerItem] = []
    @State private var selectedImages: [UIImage] = []
    
    var body: some View {
        NavigationStack {
            Form {
                // 1. KATEGORİ SEÇİMİ
                Section(header: Text("Kategori")) {
                    Picker("Kategori Seçin", selection: $selectedCategory) {
                        ForEach(AdType.allCases, id: \.self) { type in
                            Text(type.displayName).tag(type)
                        }
                    }
                    .pickerStyle(.menu)
                }
                
                // 2. TEMEL BİLGİLER
                Section(header: Text("İlan Detayları")) {
                    TextField("İlan Başlığı (Örn: Satılık Bisiklet)", text: $title)
                    TextField("Fiyat (Örn: 2.500 TL)", text: $price)
                        .keyboardType(.numbersAndPunctuation)
                    
                    TextField("Satıcı Adı / Rumuz", text: $sellerName)
                    TextField("İletişim (Tel veya E-posta)", text: $contactInfo)
                        .keyboardType(.emailAddress)
                }
                
                // 3. AÇIKLAMA
                Section(header: Text("Açıklama")) {
                    TextEditor(text: $description)
                        .frame(height: 100)
                        .overlay(
                            Text(description.isEmpty ? "Ürünün detaylarını buraya yazın..." : "")
                                .foregroundColor(.gray.opacity(0.5))
                                .padding(.top, 8)
                                .padding(.leading, 5),
                            alignment: .topLeading
                        )
                }
                
                // 4. FOTOĞRAF EKLEME
                Section(header: Text("Fotoğraflar")) {
                    PhotosPicker(
                        selection: $selectedItems,
                        maxSelectionCount: 3, // En fazla 3 fotoğraf
                        matching: .images
                    ) {
                        Label("Fotoğraf Seç (Maks 3)", systemImage: "photo.on.rectangle.angled")
                    }
                    .onChange(of: selectedItems) { newItems in
                        Task {
                            selectedImages = []
                            for item in newItems {
                                if let data = try? await item.loadTransferable(type: Data.self),
                                   let image = UIImage(data: data) {
                                    selectedImages.append(image)
                                }
                            }
                        }
                    }
                    
                    // Seçilenleri Göster
                    if !selectedImages.isEmpty {
                        ScrollView(.horizontal, showsIndicators: false) {
                            HStack {
                                ForEach(selectedImages, id: \.self) { img in
                                    Image(uiImage: img)
                                        .resizable()
                                        .scaledToFill()
                                        .frame(width: 80, height: 80)
                                        .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                            }
                        }
                        .padding(.vertical, 4)
                    }
                }
                
                // 5. GÖNDER BUTONU
                                Section {
                                    Button(action: {
                                        Task {
                                            // Basit Validasyon
                                            if title.isEmpty || price.isEmpty || contactInfo.isEmpty {
                                                // Burada bir hata mesajı alert'i gösterebilirsiniz
                                                print("Eksik bilgi")
                                                return
                                            }
                                            
                                            let success = await viewModel.submitAd(
                                                title: title,
                                                description: description,
                                                price: price,
                                                type: selectedCategory,
                                                contactInfo: contactInfo,
                                                sellerName: sellerName.isEmpty ? "İsimsiz" : sellerName,
                                                images: selectedImages
                                            )
                                            
                                            if success {
                                                dismiss() // Formu kapat
                                            }
                                        }
                                    }) {
                                        if viewModel.state == .loading {
                                            HStack {
                                                Text("Yükleniyor...")
                                                Spacer()
                                                ProgressView().tint(.white)
                                            }
                                        } else {
                                            Text("İlanı Yayına Gönder")
                                                .fontWeight(.bold)
                                                .frame(maxWidth: .infinity)
                                                .foregroundColor(.white)
                                        }
                                    }
                                    .listRowBackground(Color.red)
                                    .disabled(viewModel.state == .loading) // Yüklenirken tıklamayı engelle
                                }
                
                // BİLGİLENDİRME
                Section(footer: Text("İlanınız editör onayından sonra yayınlanacaktır.")) {
                    EmptyView()
                }
            }
            .navigationTitle("İlan Ekle")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("İptal") { dismiss() }
                }
            }
        }
    }
}
