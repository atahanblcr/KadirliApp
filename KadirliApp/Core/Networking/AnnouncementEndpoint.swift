import Foundation

enum AnnouncementEndpoint: Endpoint {
    case getAnnouncements
    
    var path: String {
        switch self {
        case .getAnnouncements:
            // En yeniden eskiye sırala
            return "/announcements?order=created_at.desc"
        }
    }
    
    var method: HTTPMethod { .GET }
}
