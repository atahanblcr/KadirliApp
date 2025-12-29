import Foundation

struct TimeHelper {
    static func timeAgo(from dateString: String?) -> String {
        guard let dateString = dateString else { return "Az önce" }
        
        // Supabase tarih formatı (ISO8601)
        let formatter = ISO8601DateFormatter()
        // Hem milisaniyeli hem milisaniyesiz formatı desteklemesi için ayar:
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        
        if let date = formatter.date(from: dateString) {
            return calculate(date: date)
        }
        
        // Eğer milisaniye yoksa (bazı durumlarda) standart formatı dene
        formatter.formatOptions = [.withInternetDateTime]
        if let date = formatter.date(from: dateString) {
            return calculate(date: date)
        }
        
        return "Az önce"
    }
    
    private static func calculate(date: Date) -> String {
        let diff = Calendar.current.dateComponents([.minute, .hour, .day], from: date, to: Date())
        
        if let day = diff.day, day > 0 {
            return "\(day) gün önce"
        } else if let hour = diff.hour, hour > 0 {
            return "\(hour) sa önce"
        } else if let minute = diff.minute, minute > 0 {
            return "\(minute) dk önce"
        } else {
            return "Şimdi"
        }
    }
}
