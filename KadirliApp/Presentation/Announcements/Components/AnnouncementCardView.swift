import SwiftUI

struct AnnouncementCardView: View {
    let announcement: Announcement
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Üst Başlık (Tip ve Kurum)
            HStack {
                HStack(spacing: 6) {
                    Image(systemName: announcement.type.iconName)
                    Text(announcement.type.title)
                }
                .font(.caption)
                .fontWeight(.bold)
                .padding(.vertical, 6)
                .padding(.horizontal, 10)
                .background(announcement.type.color.opacity(0.1))
                .foregroundColor(announcement.type.color)
                .cornerRadius(8)
                
                Spacer()
                
                Text(TimeHelper.timeAgo(from: ISO8601DateFormatter().string(from: announcement.createdAt)))
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
            
            // Başlık
            Text(announcement.title)
                .font(.headline)
                .foregroundColor(.primary)
                .lineLimit(2)
            
            // Kurum Adı
            if let institution = announcement.institutionName {
                Text(institution)
                    .font(.caption)
                    .fontWeight(.medium)
                    .foregroundColor(.gray)
            }
            
            // Mahalle Etiketi (Eğer spesifik bir mahalleyse)
            if let targets = announcement.targetNeighborhoods, !targets.isEmpty {
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack {
                        ForEach(targets, id: \.self) { neighborhood in
                            Text(neighborhood)
                                .font(.caption2)
                                .padding(4)
                                .background(Color(.systemGray6))
                                .cornerRadius(4)
                        }
                    }
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.05), radius: 4, x: 0, y: 2)
    }
}
