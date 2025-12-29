import Foundation
import UIKit // UIImage için

protocol AdsRepositoryProtocol {
    func fetchAds() async throws -> [Ad]
    func deleteAd(id: String) async throws
    // YENİ: İlan Oluşturma
    func createAd(title: String, description: String, price: String, type: String, contactInfo: String, sellerName: String, images: [UIImage], lat: Double?, long: Double?) async throws
}

final class AdsRepository: AdsRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchAds() async throws -> [Ad] {
        return try await networkManager.request(endpoint: AdsEndpoint.getActiveAds)
    }
    
    func deleteAd(id: String) async throws {
        let _: String? = try? await networkManager.request(endpoint: AdsEndpoint.softDeleteAd(id: id))
    }
    
    // YENİ: İlan Oluşturma Fonksiyonu
    func createAd(title: String, description: String, price: String, type: String, contactInfo: String, sellerName: String, images: [UIImage], lat: Double?, long: Double?) async throws {
        
        // 1. Resimleri Yükle ve URL'leri Topla
        var uploadedImageUrls: [String] = []
        
                for image in images {
                    if let imageData = image.jpegData(compressionQuality: 0.5) {
                        let fileName = "\(Int(Date().timeIntervalSince1970))_\(UUID().uuidString).jpg"
                        
                        // Resmi Yükle
                        let _: String? = try? await networkManager.request(endpoint: StorageEndpoint.uploadImage(data: imageData, fileName: fileName))
                        
                        // URL'i oluştur
                        let cleanBaseUrl = AppConfig.supabaseUrl.replacingOccurrences(of: "/rest/v1", with: "")
                        let publicUrl = "\(cleanBaseUrl)/storage/v1/object/public/ads/\(fileName)"
                        
                        // 🚨 İŞTE EKSİK OLAN KRİTİK SATIR BURASI: 🚨
                        // Bu satır olmazsa resim linki havaya uçar, veritabanına gitmez.
                        uploadedImageUrls.append(publicUrl)
                    }
                }
        
        // 2. İlan Verisini Hazırla
                let parameters: [String: Any] = [
                    "title": title,
                    "description": description,
                    "price": price,
                    "type": type,
                    "contact_info": contactInfo,
                    "seller_name": sellerName,
                    
                    // 👇 DEĞİŞİKLİK BURADA: Test için 'true' yapıyoruz.
                    // Gerçek yayına geçerken bunu tekrar 'false' yaparsın.
                    "is_active": true,
                    
                    "is_deleted": false,
                    "image_urls": uploadedImageUrls,
                    "latitude": lat ?? NSNull(),
                    "longitude": long ?? NSNull()
                ]
        
        // 3. Veritabanına Kaydet
        let _: String? = try await networkManager.request(endpoint: AdsEndpoint.createAd(adData: parameters))
    }
}
