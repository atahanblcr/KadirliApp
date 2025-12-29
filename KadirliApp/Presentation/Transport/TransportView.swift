import SwiftUI
import CoreLocation
import Combine

struct TransportView: View {
    @StateObject private var viewModel = TransportViewModel()
    @Namespace private var animation
    
    var body: some View {
        ZStack {
            Color(.systemGroupedBackground).ignoresSafeArea()
            
            VStack(spacing: 0) {
                // 1. SEKME ALANI (Boyut Sabitlendi)
                HStack(spacing: 0) {
                    tabButton(title: "Şehir İçi", index: 0)
                    tabButton(title: "Şehir Dışı", index: 1)
                }
                .frame(height: 50) // 👈 KRİTİK DÜZELTME: Yükseklik sınırlandı
                .padding(4)
                .background(Color.white)
                .cornerRadius(25)
                .padding(.horizontal)
                .padding(.top, 10)
                .shadow(color: Color.black.opacity(0.05), radius: 3)
                
                // 2. YÜKLEME DURUMU KONTROLÜ
                if viewModel.routes.isEmpty && viewModel.intercityTrips.isEmpty {
                    VStack {
                        Spacer()
                        ProgressView("Seferler Yükleniyor...")
                        Button("Tekrar Dene") {
                            Task { await viewModel.loadData() }
                        }
                        .padding(.top)
                        Spacer()
                    }
                } else {
                    // 3. İÇERİK ALANI
                    if viewModel.selectedTab == 0 {
                        InnerCityView(viewModel: viewModel)
                            .transition(.opacity)
                    } else {
                        InterCityView(viewModel: viewModel)
                            .transition(.opacity)
                    }
                }
            }
        }
        .navigationTitle("Ulaşım Saatleri")
        .navigationBarTitleDisplayMode(.inline)
        .task { await viewModel.loadData() }
    }
    
    // Tab Butonu
    @ViewBuilder
    func tabButton(title: String, index: Int) -> some View {
        Button {
            withAnimation(.spring(response: 0.4, dampingFraction: 0.7)) {
                viewModel.selectedTab = index
            }
        } label: {
            ZStack {
                if viewModel.selectedTab == index {
                    Capsule()
                        .fill(Color.red)
                        .matchedGeometryEffect(id: "TabCapsule", in: animation)
                }
                
                Text(title)
                    .font(.subheadline)
                    .fontWeight(.bold)
                    .foregroundColor(viewModel.selectedTab == index ? .white : .gray)
            }
            .frame(maxWidth: .infinity)
            .frame(maxHeight: .infinity) // Kapsayıcısına (HStack - 50px) uyar
            .contentShape(Rectangle())
        }
    }
}

// MARK: - ŞEHİR İÇİ (GÜNCELLENDİ)
struct InnerCityView: View {
    @ObservedObject var viewModel: TransportViewModel
    
    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // HAT SEÇİCİ
                if !viewModel.routes.isEmpty {
                    ScrollView(.horizontal, showsIndicators: false) {
                        HStack {
                            ForEach(viewModel.routes) { route in
                                Button(action: {
                                    Task { await viewModel.selectRoute(route) }
                                }) {
                                    Text(route.title)
                                        .font(.caption)
                                        .fontWeight(.bold)
                                        .padding(.horizontal, 16)
                                        .padding(.vertical, 8)
                                        .background(viewModel.selectedRoute?.id == route.id ? Color.blue : Color.white)
                                        .foregroundColor(viewModel.selectedRoute?.id == route.id ? .white : .primary)
                                        .cornerRadius(20)
                                        .shadow(radius: 1)
                                }
                            }
                        }
                        .padding(.horizontal)
                        .padding(.top, 10)
                    }
                }
                
                // EN YAKIN DURAK KARTI
                if let nearest = viewModel.nearestStop {
                    HStack {
                        VStack(alignment: .leading) {
                            Text("SİZE EN YAKIN DURAK")
                                .font(.caption2).bold().foregroundColor(.secondary)
                            Text(nearest.stop.name)
                                .font(.title3).bold().foregroundColor(.primary)
                        }
                        Spacer()
                        VStack(alignment: .trailing) {
                            Text("Sıradaki")
                                .font(.caption2).foregroundColor(.secondary)
                            Text(viewModel.nextBusTime)
                                .font(.title2).bold().foregroundColor(.blue)
                        }
                    }
                    .padding()
                    .background(Color.white)
                    .cornerRadius(12)
                    .padding(.horizontal)
                    .shadow(color: Color.black.opacity(0.05), radius: 5)
                }
                
                // DURAK LİSTESİ
                VStack(alignment: .leading, spacing: 0) {
                    Text("Durak Geçiş Saatleri")
                        .font(.headline)
                        .padding(.leading)
                        .padding(.bottom, 8)
                    
                    if viewModel.routeStops.isEmpty {
                        Text("Durak bilgisi bulunamadı.")
                            .padding()
                            .foregroundColor(.secondary)
                    } else {
                        ForEach(viewModel.routeStops) { item in
                            VStack(alignment: .leading, spacing: 8) {
                                HStack {
                                    Circle()
                                        .fill(item.id == viewModel.nearestStop?.id ? Color.blue : Color.gray)
                                        .frame(width: 8, height: 8)
                                    Text(item.stop.name)
                                        .fontWeight(.semibold)
                                    Spacer()
                                }
                                
                                // SAATLER (Yatay Kaydırmalı)
                                ScrollView(.horizontal, showsIndicators: false) {
                                    HStack(spacing: 8) {
                                        ForEach(viewModel.calculateStopTimes(for: item, limit: 6), id: \.self) { time in
                                            Text(time)
                                                .font(.caption)
                                                .fontWeight(.medium)
                                                .padding(.horizontal, 8)
                                                .padding(.vertical, 4)
                                                .background(Color(.systemGray6))
                                                .cornerRadius(6)
                                        }
                                    }
                                }
                            }
                            .padding()
                            .background(Color.white)
                            .overlay(Divider(), alignment: .bottom)
                        }
                    }
                }
                .cornerRadius(12)
                .padding()
            }
        }
    }
}

// MARK: - ŞEHİR DIŞI
struct InterCityView: View {
    @ObservedObject var viewModel: TransportViewModel
    
    var body: some View {
        if viewModel.intercityTrips.isEmpty {
            VStack {
                Spacer()
                Text("Şehir dışı sefer bilgisi bulunamadı.")
                    .foregroundColor(.secondary)
                Spacer()
            }
        } else {
            List {
                ForEach(viewModel.intercityTrips) { trip in
                    Section {
                        VStack(alignment: .leading, spacing: 12) {
                            HStack {
                                Text(trip.destination)
                                    .font(.title3)
                                    .fontWeight(.bold)
                                Spacer()
                                if let price = trip.price {
                                    Text(price)
                                        .font(.subheadline)
                                        .fontWeight(.bold)
                                        .padding(.horizontal, 8)
                                        .padding(.vertical, 4)
                                        .background(Color.green.opacity(0.1))
                                        .foregroundColor(.green)
                                        .cornerRadius(6)
                                }
                            }
                            Divider()
                            LazyVGrid(columns: [GridItem(.adaptive(minimum: 55))], spacing: 10) {
                                ForEach(trip.departureTimes, id: \.self) { time in
                                    Text(time)
                                        .font(.subheadline)
                                        .fontWeight(.medium)
                                        .frame(maxWidth: .infinity)
                                        .padding(.vertical, 6)
                                        .background(Color(.systemGray6))
                                        .cornerRadius(8)
                                }
                            }
                        }
                        .padding(.vertical, 4)
                    }
                }
            }
            .listStyle(.insetGrouped)
        }
    }
}
