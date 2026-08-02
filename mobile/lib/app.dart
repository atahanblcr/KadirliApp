import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/push/push_messaging.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/theme_controller.dart';
import 'features/notifications/application/push_controller.dart';

/// Uygulama kökü: tema + yönlendirme + yerelleştirme + push borusu.
class KadirliApp extends ConsumerStatefulWidget {
  const KadirliApp({super.key});

  @override
  ConsumerState<KadirliApp> createState() => _KadirliAppState();
}

class _KadirliAppState extends ConsumerState<KadirliApp> {
  /// Ön plandaki push'u `SnackBar` ile göstermek için: `MaterialApp`'in
  /// ALTINDA bir `context` gerekiyor, bu widget ise onun üstünde.
  final _messengerKey = GlobalKey<ScaffoldMessengerState>();

  @override
  void initState() {
    super.initState();
    // Push aboneliklerini kur. Sağlayıcı yapılandırılmamışsa no-op'tur,
    // hata vermez; izin/token akışı oturum açılınca (11.3 çağrısı) işler.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) ref.read(pushCoordinatorProvider).start();
    });
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(routerProvider);
    final themeMode = ref.watch(themeModeProvider);

    // Uygulama ÖN PLANDAYKEN gelen push'ta sistem bildirimi gösterilmez →
    // kullanıcı olan bitenden habersiz kalmasın diye uygulama içi şerit.
    ref.listen<PushPayload?>(foregroundPushProvider, (_, payload) {
      if (payload != null) _showForegroundPush(payload);
    });

    return MaterialApp.router(
      title: 'Kadirli',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      scaffoldMessengerKey: _messengerKey,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: themeMode,

      // Uygulama tek dilli: Türkçe (tarih/sayı biçimleri ve Material
      // metinleri de Türkçe gelsin diye locale sabitlenir).
      locale: const Locale('tr', 'TR'),
      supportedLocales: const [Locale('tr', 'TR')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],

      // Erişilebilirlik: sistem yazı boyutu saygı görür ama aşırı büyümede
      // düzen bozulmasın diye üst sınır konur (MOBILE_UX_PLAN §0.6).
      builder: (context, child) => MediaQuery.withClampedTextScaling(
        minScaleFactor: 0.9,
        maxScaleFactor: 1.4,
        child: child ?? const SizedBox.shrink(),
      ),
    );
  }

  void _showForegroundPush(PushPayload payload) {
    final coordinator = ref.read(pushCoordinatorProvider);
    final messenger = _messengerKey.currentState;
    final title = payload.title?.trim();
    final body = payload.body?.trim();

    // Yalnız `data` taşıyan sessiz mesaj (ya da messenger henüz yok) →
    // gösterilecek bir şey yok; rozet ve liste zaten tazelendi.
    if (messenger == null ||
        ((title == null || title.isEmpty) && (body == null || body.isEmpty))) {
      coordinator.clearForegroundMessage();
      return;
    }

    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          duration: const Duration(seconds: 6),
          content: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              if (title != null && title.isNotEmpty)
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              if (body != null && body.isNotEmpty)
                Text(body, maxLines: 2, overflow: TextOverflow.ellipsis),
            ],
          ),
          action: SnackBarAction(
            label: 'Görüntüle',
            onPressed: () => coordinator.openFromPush(payload),
          ),
        ),
      );

    coordinator.clearForegroundMessage();
  }
}
