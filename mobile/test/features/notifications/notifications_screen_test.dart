import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/notifications/presentation/notifications_screen.dart';
import 'package:kadirli_app/features/notifications/presentation/widgets/notification_tile.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Bildirim merkezi (11.13): liste, gün gruplaması, okundu işaretleme,
/// "yalnız okunmamışlar" filtresi ve deep-link dokunuşu.
void main() {
  const announcementId = '3f0d0a1e-6a4c-4c7e-9f1a-2b3c4d5e6f70';

  Map<String, dynamic> meBody() => {
    'id': '11111111-1111-1111-1111-111111111111',
    'phone': '+905321110001',
    'username': 'ahmetk',
    'role': 'user',
    'primaryNeighborhoodName': 'Savrun',
  };

  Map<String, dynamic> notification({
    required String id,
    String title = 'Pazar Yeri Taşınıyor',
    String body = 'Cumartesi pazarı kapalı pazar alanına taşınıyor.',
    bool isRead = false,
    String? relatedType = 'announcement',
    String? relatedId = announcementId,
    DateTime? createdAt,
  }) => {
    'id': id,
    'title': title,
    'body': body,
    'type': relatedType,
    'relatedId': relatedId,
    'relatedType': relatedType,
    'isRead': isRead,
    'readAt': null,
    'createdAt': (createdAt ?? DateTime.now().toUtc()).toIso8601String(),
  };

  Map<String, dynamic> listBody(
    List<Map<String, dynamic>> items, {
    int? unreadCount,
    int? totalCount,
  }) => successEnvelope({
    'unreadCount': unreadCount ?? items.where((i) => i['isRead'] == false).length,
    'items': items,
    'totalCount': totalCount ?? items.length,
    'pageSize': 20,
    'currentPage': 1,
    'totalPages': items.isEmpty ? 0 : 1,
  });

  /// Oturum açık kullanıcıyla Bildirimler sekmesini açar.
  Future<FakeHttpAdapter> openNotifications(
    WidgetTester tester, {
    required Map<String, Future<ResponseBody> Function(RequestOptions)> routes,
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async => jsonResponse(successEnvelope(meBody())),
      ...routes,
    });

    final container = await pumpApp(
      tester,
      tokenStore: InMemoryTokenStore(
        accessToken: 'ACCESS',
        refreshToken: 'REFRESH',
      ),
      adapter: adapter,
    );
    container.read(routerProvider).go('/bildirimler');
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('liste gelir, okunmamış satır rozetli, gün başlığı yazılır', (
    tester,
  ) async {
    await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async => jsonResponse(
          listBody([
            notification(id: 'n1'),
            notification(
              id: 'n2',
              title: 'Su kesintisi',
              isRead: true,
              createdAt: DateTime.now().toUtc().subtract(
                const Duration(days: 1),
              ),
            ),
          ]),
        ),
      },
    );

    expect(find.text('Pazar Yeri Taşınıyor'), findsOneWidget);
    expect(find.text('Su kesintisi'), findsOneWidget);
    expect(find.text('Bugün'), findsOneWidget);
    expect(find.text('Dün'), findsOneWidget);
    expect(find.byType(NotificationTile), findsNWidgets(2));
    // "Okunmadı" noktası yalnız okunmamış satırda.
    expect(find.byKey(unreadDotKey), findsOneWidget);
    expect(find.text('Toplam 2 bildirim'), findsOneWidget);
  });

  testWidgets('boş liste açıklayıcı metin gösterir', (tester) async {
    await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async => jsonResponse(listBody(const [])),
      },
    );

    expect(find.text('Henüz bildiriminiz yok'), findsOneWidget);
  });

  testWidgets('kalıcı hata tekrar dene düğmesi çıkarır', (tester) async {
    await openNotifications(
      tester,
      routes: {
        // ⚠️ 5xx `apiRetry` için GEÇİCİ hata sayılıyor → kalıcı hata (404)
        // kullanılmalı, yoksa sönmeyen zamanlayıcı kalır (11.10 dersi).
        '/v1/notifications': (_) async => jsonResponse(
          {
            'success': false,
            'error': {'code': 'NOT_FOUND', 'message': 'Bulunamadı.'},
          },
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  testWidgets('satıra dokununca okundu işaretlenir ve detaya gidilir', (
    tester,
  ) async {
    var markReadCalls = 0;
    final adapter = await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async =>
            jsonResponse(listBody([notification(id: 'n1')])),
        '/v1/notifications/n1/read': (_) async {
          markReadCalls++;
          return jsonResponse(successEnvelope({'message': 'ok'}));
        },
        '/v1/announcements/$announcementId': (_) async => jsonResponse(
          successEnvelope({
            'id': announcementId,
            'title': 'Pazar Yeri Taşınıyor',
            'body': 'Detay gövdesi.',
            'priority': 0,
            'status': 'active',
            'sentAt': DateTime.now().toUtc().toIso8601String(),
          }),
        ),
      },
    );

    await tester.tap(find.byType(NotificationTile).first);
    await tester.pumpAndSettle();

    expect(markReadCalls, 1, reason: 'PATCH …/read tam bir kez çağrılmalı');
    expect(
      adapter.requests.any((r) => r.path == '/v1/announcements/$announcementId'),
      isTrue,
      reason: 'deep-link duyuru detayını açmalı',
    );
    expect(find.text('Detay gövdesi.'), findsOneWidget);
  });

  testWidgets('okunmuş satıra dokunmak PATCH atmaz ama yine detaya götürür', (
    tester,
  ) async {
    var markReadCalls = 0;
    await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async =>
            jsonResponse(listBody([notification(id: 'n1', isRead: true)])),
        '/v1/notifications/n1/read': (_) async {
          markReadCalls++;
          return jsonResponse(successEnvelope({'message': 'ok'}));
        },
        '/v1/announcements/$announcementId': (_) async => jsonResponse(
          successEnvelope({
            'id': announcementId,
            'title': 'Pazar Yeri Taşınıyor',
            'body': 'Detay gövdesi.',
            'priority': 0,
            'status': 'active',
          }),
        ),
      },
    );

    await tester.tap(find.byType(NotificationTile).first);
    await tester.pumpAndSettle();

    expect(find.text('Detay gövdesi.'), findsOneWidget);
    expect(
      markReadCalls,
      1,
      reason:
          'push yolu kimliği bilir ama okunmuşluğu bilmez; uç idempotent olduğu '
          'için tekrar çağrı zararsız',
    );
  });

  testWidgets('"Tümünü okundu yap" uca gider ve satırlar sönükleşir', (
    tester,
  ) async {
    var readAllCalls = 0;
    await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async => jsonResponse(
          listBody([notification(id: 'n1'), notification(id: 'n2')]),
        ),
        '/v1/notifications/read-all': (_) async {
          readAllCalls++;
          return jsonResponse(successEnvelope({'markedCount': 2}));
        },
      },
    );

    expect(find.byKey(unreadDotKey), findsNWidgets(2));

    await tester.tap(find.byTooltip('Tümünü okundu yap'));
    await tester.pumpAndSettle();

    expect(readAllCalls, 1);
    expect(
      find.byKey(unreadDotKey),
      findsNothing,
      reason: 'iyimser güncelleme satırları anında okunmuş yapmalı',
    );
    expect(find.text('Tüm bildirimler okundu olarak işaretlendi.'), findsOneWidget);
  });

  testWidgets('"Okunmamışlar" filtresi uca unreadOnly parametresi gönderir', (
    tester,
  ) async {
    final adapter = await openNotifications(
      tester,
      routes: {
        '/v1/notifications': (_) async =>
            jsonResponse(listBody([notification(id: 'n1')])),
      },
    );

    await tester.tap(find.text('Okunmamışlar'));
    await tester.pumpAndSettle();

    final listRequests = adapter.requests
        .where((r) => r.path == '/v1/notifications')
        .toList();
    expect(
      listRequests.last.queryParameters['unreadOnly'],
      isTrue,
      reason: 'filtre sunucuda uygulanmalı (istemcide süzmek sayfalamayı bozar)',
    );
  });

  testWidgets('misafir kullanıcı giriş daveti görür, uca istek gitmez', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter(homeStubs());
    final container = await pumpApp(
      tester,
      prefs: {'auth.guestChoice': true},
      adapter: adapter,
    );
    container.read(routerProvider).go('/bildirimler');
    await tester.pumpAndSettle();

    expect(find.text('Bildirimleriniz burada olacak'), findsOneWidget);
    expect(
      adapter.requests.any((r) => r.path == '/v1/notifications'),
      isFalse,
      reason: 'anonimde `[A]` uca boşuna 401 üretmenin anlamı yok',
    );
  });

  group('gün gruplaması (saf mantık)', () {
    final now = DateTime.utc(2026, 8, 2, 12);

    test('bugün / dün / tam tarih', () {
      expect(notificationDayLabel(now, now: now), 'Bugün');
      expect(
        notificationDayLabel(now.subtract(const Duration(days: 1)), now: now),
        'Dün',
      );
      expect(
        notificationDayLabel(DateTime.utc(2026, 7, 20), now: now),
        contains('Temmuz'),
      );
    });

    test('tarihi olmayan kayıt listeden düşmez', () {
      expect(notificationDayLabel(null, now: now), 'Daha önce');
    });
  });
}
