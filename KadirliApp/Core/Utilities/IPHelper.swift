import Foundation

struct IPHelper {
    /// Kullanıcının o anki Public IP adresini döner.
    static func getPublicIP() async -> String? {
        // ipify servisi ücretsiz ve güvenilirdir
        guard let url = URL(string: "https://api.ipify.org") else { return nil }
        
        do {
            let (data, _) = try await URLSession.shared.data(from: url)
            return String(data: data, encoding: .utf8)
        } catch {
            print("IP alma hatası: \(error.localizedDescription)")
            return nil
        }
    }
}
