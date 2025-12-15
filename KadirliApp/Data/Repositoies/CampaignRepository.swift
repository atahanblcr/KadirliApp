import Foundation

protocol CampaignRepositoryProtocol {
    func fetchCampaigns() async throws -> [Campaign]
}

final class CampaignRepository: CampaignRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchCampaigns() async throws -> [Campaign] {
        return try await networkManager.request(endpoint: CampaignsEndpoint.getCampaigns)
    }
}
