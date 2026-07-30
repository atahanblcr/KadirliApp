import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/data/models/notification_preferences.dart';
import '../../profile/data/profile_repository.dart';

/// Bildirim anahtarlarının geçici (UI) durumu.
///
/// **Tercihlerin kendisi burada tutulmaz** — tek kaynak oturumdaki
/// `CurrentUser.notificationPreferences`. Burada yalnız "hangi anahtar şu an
/// sunucuya yazılıyor" ve "son hata" bilgisi var.
class NotificationPreferencesState {
  const NotificationPreferencesState({this.pending = const {}, this.error});

  /// İsteği süren anahtarlar (satır kilitlenir + küçük spinner).
  final Set<NotificationTopic> pending;

  /// Son başarısız yazma denemesinin mesajı (gösterilince temizlenir).
  final String? error;

  bool isPending(NotificationTopic topic) => pending.contains(topic);

  NotificationPreferencesState copyWith({
    Set<NotificationTopic>? pending,
    String? error,
    bool clearError = false,
  }) => NotificationPreferencesState(
    pending: pending ?? this.pending,
    error: clearError ? null : (error ?? this.error),
  );
}

final notificationPreferencesProvider =
    NotifierProvider<NotificationPreferencesController, NotificationPreferencesState>(
      NotificationPreferencesController.new,
    );

/// Anahtar başına **iyimser** güncelleme: kullanıcı dokunduğu an anahtar
/// yer değiştirir, istek arkada gider; sunucu reddederse eski değere geri
/// döner ve sebebi gösterilir (sessiz başarısızlık yok).
class NotificationPreferencesController extends Notifier<NotificationPreferencesState> {
  @override
  NotificationPreferencesState build() => const NotificationPreferencesState();

  Future<void> toggle(NotificationTopic topic, bool value) async {
    final user = ref.read(currentUserProvider);
    if (user == null || state.isPending(topic)) return;

    final auth = ref.read(authControllerProvider.notifier);
    final previous = user.notificationPreferences;

    state = state.copyWith(pending: {...state.pending, topic}, clearError: true);
    auth.applyProfile(
      user.copyWith(notificationPreferences: previous.withValue(topic, value)),
    );

    try {
      final updated = await ref
          .read(profileRepositoryProvider)
          .updateNotificationPreferences({topic: value});
      // Sunucunun döndüğü TÜM tercihler yazılır: başka bir cihazdan yapılmış
      // değişiklik varsa burada senkronlanır.
      _applyPreferences(updated);
      state = state.copyWith(pending: _without(topic));
    } on ApiException catch (error) {
      _applyPreferences(previous);
      state = state.copyWith(pending: _without(topic), error: error.message);
    }
  }

  void clearError() => state = state.copyWith(clearError: true);

  void _applyPreferences(NotificationPreferences preferences) {
    final current = ref.read(currentUserProvider);
    if (current == null) return;
    ref
        .read(authControllerProvider.notifier)
        .applyProfile(current.copyWith(notificationPreferences: preferences));
  }

  Set<NotificationTopic> _without(NotificationTopic topic) =>
      {...state.pending}..remove(topic);
}
