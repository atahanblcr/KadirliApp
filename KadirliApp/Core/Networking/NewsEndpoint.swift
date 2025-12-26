import Foundation

enum NewsEndpoint: Endpoint {
    // Limit (kaç tane) ve Offset (kaçıncıdan başlasın) parametreleri eklendi
    case getLatestNews(limit: Int, offset: Int)
    case getNewsDetail(id: String)
    
    var path: String {
        switch self {
        case .getLatestNews(let limit, let offset):
            // Supabase sorgusuna limit ve offset ekliyoruz
            return "/news?is_published=eq.true&order=published_at.desc&limit=\(limit)&offset=\(offset)"
        case .getNewsDetail(let id):
            return "/news?id=eq.\(id)"
        }
    }
    
    var method: HTTPMethod {
        return .GET
    }
}
