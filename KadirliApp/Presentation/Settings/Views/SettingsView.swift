import SwiftUI

struct SettingsView: View {
    @StateObject private var viewModel: SettingsViewModel
    @Environment(\.dismiss) var dismiss
    
    // Çıkış yapmak için SessionManager'a erişim
    @EnvironmentObject var sessionManager: SessionManager
    
    // Dependency Injection
    init(userId: String) {
        _viewModel = StateObject(wrappedValue: SettingsViewModel(userId: userId))
    }
    
    var body: some View {
        NavigationStack {
            Form {
                Section(header: Text("Bildirim Tercihleri"), footer: Text("Hangi konularda anlık bildirim almak istediğinizi seçin.")) {
                    
                    if viewModel.isLoading {
                        ProgressView()
                    } else {
                        Toggle("Son Dakika Haberleri", isOn: Binding(
                            get: { viewModel.preferences.news },
                            set: {
                                viewModel.preferences.news = $0
                                viewModel.updatePreference()
                            }
                        ))
                        .tint(.red)
                        
                        Toggle("Vefat İlanları", isOn: Binding(
                            get: { viewModel.preferences.deaths },
                            set: {
                                viewModel.preferences.deaths = $0
                                viewModel.updatePreference()
                            }
                        ))
                        .tint(.black)
                        
                        Toggle("Nöbetçi Eczaneler", isOn: Binding(
                            get: { viewModel.preferences.pharmacy },
                            set: {
                                viewModel.preferences.pharmacy = $0
                                viewModel.updatePreference()
                            }
                        ))
                        .tint(.green)
                        
                        Toggle("Etkinlikler", isOn: Binding(
                            get: { viewModel.preferences.events },
                            set: {
                                viewModel.preferences.events = $0
                                viewModel.updatePreference()
                            }
                        ))
                        .tint(.purple)
                    }
                }
                
                Section(header: Text("Uygulama")) {
                    HStack {
                        Text("Versiyon")
                        Spacer()
                        Text("1.0.0")
                            .foregroundColor(.secondary)
                    }
                    
                    Button("Çıkış Yap") {
                        sessionManager.logout()
                    }
                    .foregroundColor(.red)
                }
            }
            .navigationTitle("Ayarlar")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                // SOL ÜST: Kapat Butonu (YENİ EKLENDİ)
                ToolbarItem(placement: .navigationBarLeading) {
                    Button("Kapat") {
                        dismiss()
                    }
                    .foregroundColor(.red)
                }
                
                // SAĞ ÜST: Tamam Butonu
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Tamam") {
                        dismiss()
                    }
                }
            }
            .task {
                await viewModel.loadPreferences()
            }
        }
    }
}
