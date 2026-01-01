import SwiftUI

struct AnnouncementsView: View {
    @StateObject private var viewModel = AnnouncementsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            VStack(spacing: 0) {
                // Filtre Barı
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack {
                        FilterChip(title: "Tümü", isSelected: viewModel.selectedType == nil) {
                            viewModel.filterBy(type: nil)
                        }
                        
                        ForEach(AnnouncementType.allCases, id: \.self) { type in
                            FilterChip(title: type.title, isSelected: viewModel.selectedType == type) {
                                viewModel.filterBy(type: type)
                            }
                        }
                    }
                    .padding()
                }
                .background(Color(.systemBackground))
                
                // Liste
                ScrollView {
                    LazyVStack(spacing: 16) {
                        switch viewModel.state {
                        case .loading:
                            ProgressView("Duyurular alınıyor...")
                                .padding(.top, 50)
                        case .empty:
                            ContentUnavailableView("Duyuru Yok", systemImage: "bell.slash", description: Text("Şu an için aktif bir duyuru bulunmamaktadır."))
                                .padding(.top, 50)
                        case .loaded:
                            ForEach(viewModel.filteredAnnouncements) { announcement in
                                NavigationLink(destination: AnnouncementDetailView(announcement: announcement)) {
                                    AnnouncementCardView(announcement: announcement)
                                }
                                .buttonStyle(PlainButtonStyle())
                            }
                        case .error(let msg):
                            Text(msg).foregroundColor(.red).padding()
                        }
                    }
                    .padding()
                }
            }
        }
        .navigationTitle("Duyurular")
        .task {
            await viewModel.loadAnnouncements()
        }
    }
}

struct FilterChip: View {
    let title: String
    let isSelected: Bool
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            Text(title)
                .font(.subheadline)
                .fontWeight(isSelected ? .bold : .medium)
                .padding(.horizontal, 16)
                .padding(.vertical, 8)
                .background(isSelected ? Color.red : Color(.systemGray5))
                .foregroundColor(isSelected ? .white : .primary)
                .cornerRadius(20)
        }
    }
}
