import Foundation

/// Uygulama genelinde kullanılacak hata tipleri.
/// Kullanıcıya gösterilecek mesajlar Türkçe olarak yapılandırılmıştır.
enum AppError: Error, LocalizedError {
    case invalidURL
    case serverError(statusCode: Int)
    case decodingError(String)
    case unknown(Error)
    case noData
    case unauthorized

    var errorDescription: String? {
        switch self {
        case .invalidURL:
            return "Geçersiz URL yapısı. Lütfen geliştirici ile iletişime geçin."
        case .serverError(let statusCode):
            return "Sunucu hatası oluştu. Kod: \(statusCode). Lütfen daha sonra tekrar deneyin."
        case .decodingError(let message):
            return "Veri işlenirken bir hata oluştu: \(message)"
        case .unknown(let error):
            return "Bilinmeyen bir hata oluştu: \(error.localizedDescription)"
        case .noData:
            return "Sunucudan veri alınamadı."
        case .unauthorized:
            return "Bu işlem için yetkiniz bulunmuyor. Lütfen giriş yapın."
        }
    }
}
