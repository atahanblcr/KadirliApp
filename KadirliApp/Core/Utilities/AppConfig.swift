import Foundation

struct AppConfig {
    // Info.plist içinden SupabaseURL'i okur
    static var supabaseUrl: String {
        return Bundle.main.object(forInfoDictionaryKey: "SupabaseURL") as? String ?? ""
    }
    
    // Info.plist içinden SupabaseKey'i okur
    static var supabaseKey: String {
        return Bundle.main.object(forInfoDictionaryKey: "SupabaseKey") as? String ?? ""
    }
}

