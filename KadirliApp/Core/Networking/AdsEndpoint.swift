import Foundation

enum AdsEndpoint: Endpoint {
    case getActiveAds
    // İleride silme işlemi için bu case'i kullanacağız
    case softDeleteAd(id: String)
    
    var path: String {
        switch self {
        case .getActiveAds:
            // DEĞİŞİKLİK BURADA:
            // is_active=eq.true YANINA &is_deleted=eq.false ekledik.
            // Böylece silinmiş olarak işaretlenenler asla gelmeyecek.
            return "/ads?is_active=eq.true&is_deleted=eq.false&order=created_at.desc"
            
        case .softDeleteAd(let id):
            return "/ads?id=eq.\(id)"
        }
    }
    
    var method: HTTPMethod {
        switch self {
        case .getActiveAds: return .GET
        case .softDeleteAd: return .PATCH // Güncelleme işlemi
        }
    }
    
    var body: Data? {
        switch self {
        case .getActiveAds:
            return nil
        case .softDeleteAd:
            // Sadece is_deleted alanını true yapıyoruz
            let params = ["is_deleted": true]
            return try? JSONSerialization.data(withJSONObject: params)
        }
    }
}
