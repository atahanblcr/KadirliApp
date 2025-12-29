import Foundation
import SwiftUI
import Combine
import CoreLocation

@MainActor
class TransportViewModel: ObservableObject {
    @Published var selectedTab: Int = 0
    @Published var routes: [TransportRoute] = []
    @Published var selectedRoute: TransportRoute?
    @Published var routeStops: [RouteStopItem] = []
    @Published var intercityTrips: [IntercityTrip] = []
    @Published var nearestStop: RouteStopItem?
    @Published var nextBusTime: String = "--:--"
    
    private let repository = TransportRepository()
    private let locationManager = LocationManager()
    
    func loadData() async {
        locationManager.requestLocation()
        do {
            async let fetchedRoutes = repository.fetchRoutes()
            async let fetchedIntercity = repository.fetchIntercityTrips()
            
            self.routes = try await fetchedRoutes
            self.intercityTrips = try await fetchedIntercity
            
            if let firstRoute = routes.first {
                await selectRoute(firstRoute)
            }
        } catch {
            print("Veri hatası: \(error)")
        }
    }
    
    func selectRoute(_ route: TransportRoute) async {
        self.selectedRoute = route
        do {
            let stops = try await repository.fetchStops(for: route.id.uuidString)
            self.routeStops = stops
            calculateNearestStop()
        } catch {
            print("Durak hatası: \(error)")
        }
    }
    
    func calculateNearestStop() {
        guard let userLoc = locationManager.location, !routeStops.isEmpty else { return }
        
        let sortedStops = routeStops.sorted { item1, item2 in
            let loc1 = CLLocation(latitude: item1.stop.latitude, longitude: item1.stop.longitude)
            let loc2 = CLLocation(latitude: item2.stop.latitude, longitude: item2.stop.longitude)
            return userLoc.distance(from: loc1) < userLoc.distance(from: loc2)
        }
        
        if let nearest = sortedStops.first {
            self.nearestStop = nearest
            // En yakındaki durağın bir sonraki saatini hesapla
            let times = calculateStopTimes(for: nearest, limit: 1)
            self.nextBusTime = times.first ?? "Bitti"
        }
    }
    
    // 🔥 YENİ: Gerçek Saatleri Hesaplayan Fonksiyon
    // "+10 dk" mantığını alır, "08:10, 08:40..." serisine çevirir.
    func calculateStopTimes(for stopItem: RouteStopItem, limit: Int = 10) -> [String] {
        guard let route = selectedRoute else { return [] }
        
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "HH:mm:ss"
        
        // Başlangıç ve Bitiş saatlerini al
        guard let startDate = dateFormatter.date(from: route.startTime),
              let endDate = dateFormatter.date(from: route.endTime) else { return [] }
        
        let now = Date()
        let calendar = Calendar.current
        
        // Bugünün tarihine, veritabanındaki saati monte et
        let todayStart = calendar.date(bySettingHour: calendar.component(.hour, from: startDate),
                                       minute: calendar.component(.minute, from: startDate),
                                       second: 0, of: now)!
        
        let todayEnd = calendar.date(bySettingHour: calendar.component(.hour, from: endDate),
                                     minute: calendar.component(.minute, from: endDate),
                                     second: 0, of: now)!
        
        var calculatedTimes: [String] = []
        var tripTime = todayStart
        
        // Seferleri döngüye sok
        while tripTime <= todayEnd {
            // Bu durağa varış saati = Kalkış + Offset (dk)
            if let arrivalTime = calendar.date(byAdding: .minute, value: stopItem.minutesFromStart, to: tripTime) {
                
                // Sadece şu andan sonraki seferleri göster
                if arrivalTime > now {
                    let outputFormatter = DateFormatter()
                    outputFormatter.dateFormat = "HH:mm"
                    calculatedTimes.append(outputFormatter.string(from: arrivalTime))
                }
            }
            
            // Limit dolduysa dur (Performans için)
            if calculatedTimes.count >= limit { break }
            
            // Bir sonraki sefere geç
            tripTime = calendar.date(byAdding: .minute, value: route.frequencyMin, to: tripTime)!
        }
        
        return calculatedTimes.isEmpty ? ["Sefer Bitti"] : calculatedTimes
    }
}
