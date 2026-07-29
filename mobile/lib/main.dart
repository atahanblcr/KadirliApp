import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'core/theme/theme_controller.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Dikey kilit: liste/form ağırlıklı bir uygulama, yatay düzen tasarlanmadı.
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  // Türkçe tarih adları (intl) — "12 Ağustos Salı" gibi biçimler için.
  await initializeDateFormatting('tr_TR');

  // Tema tercihi ilk kareden itibaren doğru olsun diye açılışta okunur.
  final prefs = await SharedPreferences.getInstance();

  runApp(
    ProviderScope(
      overrides: [sharedPreferencesProvider.overrideWithValue(prefs)],
      child: const KadirliApp(),
    ),
  );
}
