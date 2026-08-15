import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/auth/data/auth_repository.dart';
import 'package:kadirli_app/features/legal/data/legal_repository.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Auth uçlarının **gerçek** yanıt gövdeleriyle eşleşmesi (canlı curl
/// çıktılarından alındı, 30 Tem 2026).
void main() {
  AuthRepository repositoryWith(FakeHttpAdapter adapter, {TokenStore? tokenStore}) =>
      AuthRepository(testApiClient(adapter, tokenStore: tokenStore));

  group('requestOtp', () {
    test('dev modda dönen gövdeyi ayrıştırır (message/expiresIn/retryAfter/otp)', () async {
      final adapter = routedAdapter({
        '/v1/auth/login': (_) async => jsonResponse(
          successEnvelope({
            'message': 'OTP gönderildi',
            'expiresIn': 300,
            'retryAfter': 60,
            'otp': '123456',
          }),
        ),
      });

      final challenge = await repositoryWith(adapter).requestOtp('+905321110001');

      expect(challenge.expiresIn, 300);
      expect(challenge.retryAfter, 60);
      expect(challenge.hasDevOtp, isTrue);
      expect(challenge.otp, '123456');
      expect(challenge.resendCooldown, const Duration(seconds: 60));

      expect(adapter.lastOf('/v1/auth/login')!.data, {'phone': '+905321110001'});
    });

    test('prod gövdesinde otp alanı yok → hasDevOtp false', () async {
      final adapter = routedAdapter({
        '/v1/auth/login': (_) async => jsonResponse(
          successEnvelope({'message': 'OTP gönderildi', 'expiresIn': 300, 'retryAfter': 60}),
        ),
      });

      final challenge = await repositoryWith(adapter).requestOtp('+905321110001');

      expect(challenge.hasDevOtp, isFalse);
      expect(challenge.otp, isNull);
    });

    test('hız limiti ApiException olarak gelir', () async {
      final adapter = routedAdapter({
        '/v1/auth/login': (_) async => jsonResponse(
          errorEnvelope('RATE_LIMITED', 'Çok fazla OTP isteği.'),
          statusCode: 429,
          headers: {
            'retry-after': ['30'],
          },
        ),
      });

      await expectLater(
        repositoryWith(adapter).requestOtp('+905321110001'),
        throwsA(
          isA<ApiException>()
              .having((e) => e.isRateLimited, 'isRateLimited', isTrue)
              .having((e) => e.retryAfter, 'retryAfter', const Duration(seconds: 30)),
        ),
      );
    });
  });

  group('verifyOtp', () {
    test('kayıtlı kullanıcı → token çifti', () async {
      final adapter = routedAdapter({
        '/v1/auth/verify-otp': (_) async => jsonResponse(
          successEnvelope({
            'isNewUser': false,
            'accessToken': 'ACCESS',
            'refreshToken': 'REFRESH',
            'expiresIn': 86400,
          }),
        ),
      });

      final result = await repositoryWith(adapter).verifyOtp(
        phoneE164: '+905321110001',
        otp: '123456',
      );

      expect(result.isNewUser, isFalse);
      expect(result.tokens?.accessToken, 'ACCESS');
      expect(result.tokens?.refreshToken, 'REFRESH');
    });

    test('yeni kullanıcı → tempToken, token çifti yok', () async {
      final adapter = routedAdapter({
        '/v1/auth/verify-otp': (_) async =>
            jsonResponse(successEnvelope({'isNewUser': true, 'tempToken': 'TEMP'})),
      });

      final result = await repositoryWith(adapter).verifyOtp(
        phoneE164: '+905339990001',
        otp: '123456',
      );

      expect(result.isNewUser, isTrue);
      expect(result.tempToken, 'TEMP');
      expect(result.tokens, isNull);
    });

    test('hatalı kod → INVALID_OTP (yenileme denenmez)', () async {
      final adapter = routedAdapter({
        '/v1/auth/verify-otp': (_) async => jsonResponse(
          errorEnvelope('INVALID_OTP', 'Geçersiz veya süresi dolmuş OTP.'),
          statusCode: 400,
        ),
      });

      await expectLater(
        repositoryWith(
          adapter,
          tokenStore: InMemoryTokenStore(accessToken: 'A', refreshToken: 'R'),
        ).verifyOtp(phoneE164: '+905321110001', otp: '000000'),
        throwsA(isA<ApiException>().having((e) => e.code, 'code', ApiErrorCodes.invalidOtp)),
      );
      expect(adapter.countOf('/v1/auth/refresh'), 0);
    });
  });

  test('register gövdesi kontrata uygun gider', () async {
    final adapter = routedAdapter({
      '/v1/auth/register': (_) async => jsonResponse(
        successEnvelope({
          'accessToken': 'ACCESS',
          'refreshToken': 'REFRESH',
          'expiresIn': 86400,
        }),
      ),
    });

    final tokens = await repositoryWith(adapter).register(
      tempToken: 'TEMP',
      username: 'atahan',
      primaryNeighborhoodId: 'e5b0a7f0-0000-0000-0000-000000000001',
      age: 30,
      // Faz 12.17 — KVKK kararları. ⚠️ `granted: false` de gönderilir:
      // "sormadık" ile "sorduk, hayır dedi" KVKK'da farklı şeylerdir ve
      // yalnız `true` yollansaydı bu fark hiçbir yerde durmazdı.
      consents: const [
        ConsentDecision(versionId: 'v1-0000-0000-0000-000000000001', granted: true),
        ConsentDecision(versionId: 'v2-0000-0000-0000-000000000002', granted: false),
      ],
    );

    expect(tokens.accessToken, 'ACCESS');
    expect(adapter.lastOf('/v1/auth/register')!.data, {
      'tempToken': 'TEMP',
      'username': 'atahan',
      'primaryNeighborhoodId': 'e5b0a7f0-0000-0000-0000-000000000001',
      'age': 30,
      'consents': [
        {'versionId': 'v1-0000-0000-0000-000000000001', 'granted': true},
        {'versionId': 'v2-0000-0000-0000-000000000002', 'granted': false},
      ],
    });
  });

  test('rıza verilmediyse consents BOŞ DİZİ gider (alan hiç düşmez)', () async {
    // 🔑 Alan **additive** (§5): taze kurulumda yayında zorunlu belge yok ve
    // kayıt akışı birebir 12.17 öncesi gibi çalışmalı. Alanın gövdeden tamamen
    // düşmesi de çalışırdı ama boş dizi göndermek niyeti **açık** kılıyor:
    // "sorduk, hiçbir belge yoktu" ile "hiç sormadık" sunucuda aynı yola gider,
    // bizde ayrışmasın diye tek biçim var.
    final adapter = routedAdapter({
      '/v1/auth/register': (_) async => jsonResponse(
        successEnvelope({
          'accessToken': 'ACCESS',
          'refreshToken': 'REFRESH',
          'expiresIn': 86400,
        }),
      ),
    });

    await repositoryWith(adapter).register(
      tempToken: 'TEMP',
      username: 'atahan',
      primaryNeighborhoodId: 'e5b0a7f0-0000-0000-0000-000000000001',
    );

    expect(
      (adapter.lastOf('/v1/auth/register')!.data as Map)['consents'],
      isEmpty,
    );
  });

  test('logout refresh token gövdede gider ve Bearer eklenir', () async {
    final adapter = routedAdapter({
      '/v1/auth/logout': (_) async => jsonResponse(
        successEnvelope({'message': 'Çıkış yapıldı'}),
      ),
    });

    await repositoryWith(
      adapter,
      tokenStore: InMemoryTokenStore(accessToken: 'ACCESS', refreshToken: 'REFRESH'),
    ).logout(refreshToken: 'REFRESH');

    final request = adapter.lastOf('/v1/auth/logout')!;
    expect(request.data, {'refreshToken': 'REFRESH'});
    expect(request.headers['Authorization'], 'Bearer ACCESS');
  });

  test('fetchCurrentUser kısmi modeli ayrıştırır (fazla alanlar yok sayılır)', () async {
    final adapter = routedAdapter({
      '/v1/users/me': (_) async => jsonResponse(
        successEnvelope({
          'id': '11111111-1111-1111-1111-111111111111',
          'phone': '+905321110001',
          'username': 'ahmetk',
          'role': 'user',
          'age': 34,
          'primaryNeighborhoodId': '22222222-2222-2222-2222-222222222222',
          'primaryNeighborhoodName': 'Savrun',
          'profilePhotoUrl': null,
          // 11.5'in alanları — bugün modelde yok, ayrıştırma patlamamalı:
          'notificationPreferences': {'announcements': true, 'deaths': true},
          'usernameLastChangedAt': null,
          'createdAt': '2026-07-01T10:00:00.0000000Z',
        }),
      ),
    });

    final user = await repositoryWith(adapter).fetchCurrentUser();

    expect(user.username, 'ahmetk');
    expect(user.displayName, 'ahmetk');
    expect(user.primaryNeighborhoodName, 'Savrun');
    expect(user.isStandardUser, isTrue);
    expect(user.createdAt?.year, 2026);
  });
}
