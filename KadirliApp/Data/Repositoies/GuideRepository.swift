import Foundation

protocol GuideRepositoryProtocol {
    func fetchCategories() async throws -> [GuideCategory]
    func fetchItems(categoryId: String) async throws -> [GuideItem]
}

final class GuideRepository: GuideRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func fetchCategories() async throws -> [GuideCategory] {
        return try await networkManager.request(endpoint: GuideEndpoint.getCategories)
    }
    
    func fetchItems(categoryId: String) async throws -> [GuideItem] {
        return try await networkManager.request(endpoint: GuideEndpoint.getItems(categoryId: categoryId))
    }
}
