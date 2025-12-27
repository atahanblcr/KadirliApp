import SwiftUI

struct GuideCategoriesView: View {
    @StateObject private var viewModel = GuideViewModel()
    
    // Grid Düzeni
    let columns = [
        GridItem(.flexible(), spacing: 16),
        GridItem(.flexible(), spacing: 16)
    ]
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            ScrollView {
                LazyVGrid(columns: columns, spacing: 16) {
                    switch viewModel.state {
                    case .loading:
                        ProgressView().padding(.top, 50)
                    case .error(let msg):
                        Text("Hata: \(msg)").foregroundColor(.red)
                    case .loaded:
                        ForEach(viewModel.categories) { category in
                            NavigationLink(destination: GuideListView(category: category)) {
                                GuideCategoryCard(category: category)
                            }
                            .buttonStyle(PlainButtonStyle())
                        }
                    default: EmptyView()
                    }
                }
                .padding()
            }
        }
        .navigationTitle("Altın Rehber")
        .task { await viewModel.loadCategories() }
    }
}

// Kart Tasarımı
struct GuideCategoryCard: View {
    let category: GuideCategory
    
    var body: some View {
        VStack(spacing: 12) {
            Image(systemName: category.iconName ?? "book.fill")
                .font(.system(size: 32))
                .foregroundColor(.white)
                .frame(width: 60, height: 60)
                .background(Color.orange.opacity(0.8)) // Altın Rehber Rengi :)
                .clipShape(Circle())
            
            Text(category.title)
                .font(.headline)
                .fontWeight(.bold)
                .multilineTextAlignment(.center)
                .foregroundColor(.primary)
        }
        .frame(maxWidth: .infinity)
        .frame(height: 140)
        .background(Color(.systemBackground))
        .cornerRadius(16)
        .shadow(color: Color.black.opacity(0.05), radius: 5, x: 0, y: 2)
    }
}
