import Foundation

protocol AuthRepositoryProtocol {
    func login(email: String, pass: String) async throws -> AuthResponseDTO
    func register(email: String, pass: String, fullName: String) async throws -> AuthResponseDTO
}

final class AuthRepository: AuthRepositoryProtocol {
    private let networkManager = NetworkManager.shared
    
    func login(email: String, pass: String) async throws -> AuthResponseDTO {
        return try await networkManager.request(endpoint: AuthEndpoint.login(email: email, pass: pass))
    }
    
    func register(email: String, pass: String, fullName: String) async throws -> AuthResponseDTO {
        return try await networkManager.request(endpoint: AuthEndpoint.register(email: email, pass: pass, fullName: fullName))
    }
}
