import SwiftUI

struct PharmacyCardView: View {
    let pharmacy: Pharmacy
    let onCall: () -> Void
    let onMap: () -> Void
    
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Üst Kısım: İsim ve Bölge
            HStack {
                VStack(alignment: .leading, spacing: 4) {
                    Text(pharmacy.name)
                        .font(.title3)
                        .fontWeight(.bold)
                        .foregroundColor(.primary)
                    
                    if let address = pharmacy.address {
                        Text(address)
                            .font(.caption)
                            .foregroundColor(.secondary)
                            .lineLimit(2)
                    }
                }
                Spacer()
                
                // Nöbet Tarihi Badge
                Text("Nöbetçi")
                    .font(.caption2)
                    .fontWeight(.bold)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(Color.red.opacity(0.1))
                    .foregroundColor(.red)
                    .cornerRadius(4)
            }
            
            Divider()
            
            // Alt Kısım: Aksiyon Butonları
            HStack(spacing: 12) {
                // 1. Ara Butonu
                Button(action: onCall) {
                    HStack {
                        Image(systemName: "phone.fill")
                        Text("Ara")
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 10)
                    .background(Color.green)
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
                
                // 2. Yol Tarifi Butonu
                Button(action: onMap) {
                    HStack {
                        Image(systemName: "map.fill")
                        Text("Yol Tarifi")
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 10)
                    .background(Color.blue)
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(color: Color.black.opacity(0.05), radius: 4, x: 0, y: 2)
    }
}
