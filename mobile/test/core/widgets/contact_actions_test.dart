import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

/// `ContactActions` (11.7'de çıkarıldı; eczane + rehber kullanıyor, **11.11
/// taksi + mekanlar da kullanacak**).
///
/// Bileşenin tek sözü var: **veri yoksa buton hiç çizilmez** (MOBILE_UX_PLAN
/// "işlevsiz buton yok" şartı). Bugüne dek yalnız pozitif hâli dolaylı olarak
/// test ediliyordu; bu dosya kuralın kendisini kilitliyor.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const channel = MethodChannel('plugins.flutter.io/url_launcher');
  late bool canLaunch;

  setUp(() {
    canLaunch = true;
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          if (call.method == 'canLaunch') return canLaunch;
          if (call.method == 'launch') return canLaunch;
          return null;
        });
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, null);
  });

  Future<void> pump(WidgetTester tester, ContactActions actions) =>
      tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: Scaffold(body: Center(child: actions)),
        ),
      );

  testWidgets('telefon yoksa "Ara" butonu çizilmez', (tester) async {
    await pump(tester, const ContactActions(address: 'Cumhuriyet Mah.'));

    expect(find.text('Ara'), findsNothing);
    expect(find.text('Yol tarifi'), findsOneWidget);
  });

  testWidgets('boşluktan ibaret telefon veri sayılmaz', (tester) async {
    await pump(tester, const ContactActions(phone: '   '));

    expect(find.text('Ara'), findsNothing);
  });

  testWidgets('hiç veri yoksa bileşen tamamen boş döner', (tester) async {
    await pump(tester, const ContactActions());

    expect(find.byType(AppButton), findsNothing);
  });

  testWidgets('koordinat yoksa adresle yol tarifi yine sunulur', (tester) async {
    await pump(tester, const ContactActions(address: 'Kadirli merkez'));

    expect(find.text('Yol tarifi'), findsOneWidget);
  });

  testWidgets('ne koordinat ne adres varsa yol tarifi çizilmez', (tester) async {
    await pump(tester, const ContactActions(phone: '05321110001'));

    expect(find.text('Ara'), findsOneWidget);
    expect(find.text('Yol tarifi'), findsNothing);
  });

  testWidgets('web ve e-posta yalnız dolu geldiğinde görünür', (tester) async {
    await pump(
      tester,
      const ContactActions(
        phone: '05321110001',
        website: 'kadirli.bel.tr',
        email: '',
      ),
    );

    expect(find.text('Web sitesi'), findsOneWidget);
    expect(find.text('E-posta'), findsNothing);
  });

  testWidgets('callLabel bağlama özel yazılabilir', (tester) async {
    await pump(
      tester,
      const ContactActions(phone: '05321110001', callLabel: 'Eczaneyi ara'),
    );

    expect(find.text('Eczaneyi ara'), findsOneWidget);
  });

  testWidgets('dış uygulama açılamazsa sessiz kalınmaz, şerit gösterilir', (
    tester,
  ) async {
    canLaunch = false;
    await pump(tester, const ContactActions(phone: '05321110001'));

    await tester.tap(find.text('Ara'));
    await tester.pumpAndSettle();

    expect(find.text('Arama başlatılamadı.'), findsOneWidget);
  });

  testWidgets('büyük yazı ölçeğinde butonlar taşmaz', (tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          // Uygulamanın üst sınırı (`MediaQuery.withClampedTextScaling`).
          data: const MediaQueryData(textScaler: TextScaler.linear(1.4)),
          child: const Scaffold(
            body: Center(
              child: ContactActions(
                phone: '05321110001',
                latitude: 37.37,
                longitude: 36.09,
                website: 'kadirli.bel.tr',
                email: 'bilgi@kadirli.bel.tr',
                callLabel: 'Eczaneyi ara',
              ),
            ),
          ),
        ),
      ),
    );

    expect(tester.takeException(), isNull);
  });
}
