import Foundation

enum NewsEndpoint: Endpoint {
    case getLatestNews
    case getNewsDetail(id: String)
    
    var path: String {
        switch self {
        case .getLatestNews:
            // Supabase'de 'news' tablosundan, yayınlanmış olanları, tarihe göre azalan sırada çek
            return "/news?is_published=eq.true&order=published_at.desc"
        case .getNewsDetail(let id):
            return "/news?id=eq.\(id)"
        }
    }
    
    var method: HTTPMethod {
        return .GET
    }
}
