import Foundation
import Combine

@MainActor
final class CampaignsViewModel: ObservableObject {
    @Published var campaigns: [Campaign] = []
    @Published var state: ViewState = .loading
    
    private let repository: CampaignRepositoryProtocol
    
    init(repository: CampaignRepositoryProtocol? = nil) {
        self.repository = repository ?? CampaignRepository()
    }
    
    func loadCampaigns() async {
        state = .loading
        do {
            let result = try await repository.fetchCampaigns()
            if result.isEmpty {
                state = .empty
            } else {
                campaigns = result
                state = .loaded
            }
        } catch {
            state = .error(error.localizedDescription)
        }
    }
}
