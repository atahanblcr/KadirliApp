import Foundation

/// Tüm View'larda ortak kullanılacak yükleme durumları.
/// Equatable protokolü eklendiği için "if state == .loading" gibi kontroller yapılabilir.
enum ViewState: Equatable {
    case loading        // Yükleniyor
    case loaded         // Yüklendi
    case empty          // Veri yok
    case error(String)  // Hata var (Mesaj içerir)
}
