import Foundation
import SwiftUI
import Combine

@MainActor
final class PharmacyViewModel: ObservableObject {
    @Published var state: ViewState = .loading
    
    // İki ayrı liste
    @Published var currentPharmacies: [Pharmacy] = []
    @Published var nextPharmacies: [Pharmacy] = []
    
    private let repository: PharmacyRepositoryProtocol
    
    init(repository: PharmacyRepositoryProtocol? = nil) {
        self.repository = repository ?? PharmacyRepository()
    }
    
    func loadPharmacies() async {
        state = .loading
        do {
            let all = try await repository.fetchDutyPharmacies()
            
            // Veri varsa işle, yoksa boş dön
            if let firstDate = all.first?.dutyDate {
                self.currentPharmacies = all.filter { $0.dutyDate == firstDate }
                self.nextPharmacies = all.filter { $0.dutyDate != firstDate }
            } else {
                self.currentPharmacies = []
                self.nextPharmacies = []
            }
            
            state = .loaded
        } catch {
            state = .error(error.localizedDescription)
        }
    }
    
    // MARK: - Aksiyonlar
    
    func callPharmacy(phone: String?) {
        guard let phone = phone else { return }
        let cleanPhone = phone.components(separatedBy: CharacterSet.decimalDigits.inverted).joined()
        if let url = URL(string: "tel://\(cleanPhone)"), UIApplication.shared.canOpenURL(url) {
            UIApplication.shared.open(url)
        }
    }
    
    func openMap(lat: Double?, long: Double?, name: String) {
        guard let lat = lat, let long = long else { return }
        let urlString = "http://maps.apple.com/?daddr=\(lat),\(long)&dirflg=d&t=m"
        if let url = URL(string: urlString), UIApplication.shared.canOpenURL(url) {
            UIApplication.shared.open(url)
        }
    }
}
