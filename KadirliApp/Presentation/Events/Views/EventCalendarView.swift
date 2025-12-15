import SwiftUI

struct EventCalendarView: View {
    @StateObject private var viewModel = EventsViewModel()
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            VStack(spacing: 0) {
                // 1. Segmented Control
                Picker("Filtre", selection: $viewModel.selectedFilter) {
                    ForEach(EventFilter.allCases, id: \.self) { filter in
                        Text(filter.rawValue).tag(filter)
                    }
                }
                .pickerStyle(.segmented)
                .padding()
                .background(Color(.systemBackground))
                
                // 2. Liste
                ScrollView {
                    LazyVStack(spacing: 16) {
                        switch viewModel.state {
                        case .loading:
                            ProgressView("Etkinlikler Yükleniyor...")
                                .padding(.top, 50)
                            
                        case .loaded:
                            // ...
                            ForEach(viewModel.currentDisplayList) { event in
                                NavigationLink(destination: EventDetailView(event: event)) {
                                    EventCardView(event: event)
                                }
                                .buttonStyle(PlainButtonStyle()) // Kartın mavi olmasını engeller
                                .padding(.horizontal)
                            }
                            // ...
                            
                        case .empty:
                            emptyStateView
                            
                        case .error(let message):
                            Text("Hata: \(message)").foregroundColor(.red).padding()
                        }
                    }
                    .padding(.vertical)
                }
            }
        }
        .navigationTitle("Etkinlik Takvimi")
        .navigationBarTitleDisplayMode(.inline)
        .task {
            await viewModel.loadEvents()
        }
    }
    
    var emptyStateView: some View {
        VStack(spacing: 12) {
            Image(systemName: "calendar.badge.exclamationmark")
                .font(.system(size: 50))
                .foregroundColor(.gray)
            Text(viewModel.selectedFilter == .today ? "Bugün için etkinlik yok." : "Yaklaşan etkinlik bulunmuyor.")
                .foregroundColor(.secondary)
        }
        .padding(.top, 60)
    }
}
