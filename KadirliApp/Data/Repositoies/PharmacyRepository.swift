import Foundation

protocol PharmacyRepositoryProtocol {
    func fetchDutyPharmacies() async throws -> [Pharmacy]
}

final class PharmacyRepository: PharmacyRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    private let dateFormatter: DateFormatter
    
    init() {
        self.dateFormatter = DateFormatter()
        self.dateFormatter.dateFormat = "yyyy-MM-dd"
    }
    
    func fetchDutyPharmacies() async throws -> [Pharmacy] {
        let now = Date()
        var calendar = Calendar.current
        calendar.locale = Locale(identifier: "tr_TR")
        
        let hour = calendar.component(.hour, from: now)
        
        let queryDate: Date
        if hour < 9 {
            // Gece 09:00'dan önceyse bir önceki günü al (Dünkü nöbetçiler devam ediyor)
            queryDate = calendar.date(byAdding: .day, value: -1, to: now)!
        } else {
            queryDate = now
        }
        
        let dateStr = dateFormatter.string(from: queryDate)
        
        return try await networkManager.request(endpoint: PharmacyEndpoint.getDutyPharmacies(date: dateStr))
    }
}
