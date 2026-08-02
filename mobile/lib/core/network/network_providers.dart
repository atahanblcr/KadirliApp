import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';
import 'connectivity_status.dart';
import 'dio_client.dart';
import 'token_store.dart';

/// Token deposu. Testlerde [InMemoryTokenStore] ile override edilir.
final tokenStoreProvider = Provider<TokenStore>((ref) => SecureTokenStore());

/// Oturum düştüğünde (refresh de reddedildi) artan sayaç.
///
/// 11.3'te `go_router` bunu dinleyip kullanıcıyı Giriş ekranına yönlendirecek;
/// ağ katmanı yönlendirmeyi kendisi yapmaz (katman ayrımı).
final sessionExpiredProvider = NotifierProvider<SessionExpiredNotifier, int>(
  SessionExpiredNotifier.new,
);

class SessionExpiredNotifier extends Notifier<int> {
  @override
  int build() => 0;

  void signal() => state++;
}

/// Yapılandırılmış Dio (auth + zarf + dev günlüğü).
final dioProvider = Provider<Dio>((ref) {
  final dio = DioClient.create(
    tokenStore: ref.watch(tokenStoreProvider),
    onSessionExpired: () => ref.read(sessionExpiredProvider.notifier).signal(),
    onReachable: () => ref.read(connectivityStatusProvider.notifier).goOnline(),
    onUnreachable: () =>
        ref.read(connectivityStatusProvider.notifier).goOffline(),
  );
  ref.onDispose(dio.close);
  return dio;
});

/// Repository'lerin bağımlı olduğu istemci.
final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(ref.watch(dioProvider)));
