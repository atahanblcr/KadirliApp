import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../auth/application/auth_controller.dart';
import '../data/notifications_repository.dart';

/// Alt sekme rozeti: okunmamış bildirim sayısı.
///
/// Uç `[A]` olduğu için **anonim kullanıcıda hiç istek atılmaz** (0 döner).
/// Oturum açılıp kapandığında `authControllerProvider` değiştiği için provider
/// kendiliğinden yeniden hesaplanır.
final unreadNotificationCountProvider = FutureProvider<int>((ref) async {
  final isAuthenticated = ref.watch(
    authControllerProvider.select((state) => state.isAuthenticated),
  );
  if (!isAuthenticated) return 0;

  return ref.watch(notificationsRepositoryProvider).unreadCount();
}, retry: apiRetry);
