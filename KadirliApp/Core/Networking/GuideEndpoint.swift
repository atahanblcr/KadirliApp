import Foundation

enum GuideEndpoint: Endpoint {
    case getCategories
    case getItems(categoryId: String)
    
    var path: String {
        switch self {
        case .getCategories:
            // Sıralamaya göre kategorileri getir
            return "/guide_categories?order=rank.asc"
        case .getItems(let categoryId):
            // Seçili kategorinin elemanlarını getir (Sıralı)
            return "/guide_items?category_id=eq.\(categoryId)&order=rank.asc"
        }
    }
    
    var method: HTTPMethod { .GET }
}
