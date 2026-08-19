import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../preferences/app_preferences.dart';

// 📌 12.23: `sharedPreferencesProvider` buradan `core/preferences/app_preferences.dart`'a
// taşındı. Bir altyapı provider'ının sahibi tema denetleyicisi olamazdı — dört ayrı
// özellik ondan `show sharedPreferencesProvider` diye içe aktarıyordu ve 12.23 deponun
// **bozulma durumunu** taşıyan ikinci bir provider ekledi; ikisinin de yeri orası.

/// Kullanıcının tema tercihi: Açık / Koyu / Sistem (MOBILE_UX_PLAN §5 — Ayarlar).
///
/// İstemci tarafı, kalıcı (`shared_preferences`). Ayarlar ekranı 11.5'te bunu kullanır.
final themeModeProvider = NotifierProvider<ThemeModeController, ThemeMode>(
  ThemeModeController.new,
);

class ThemeModeController extends Notifier<ThemeMode> {
  static const _prefsKey = 'settings.themeMode';

  @override
  ThemeMode build() {
    final stored = ref.read(sharedPreferencesProvider).getString(_prefsKey);
    return _decode(stored);
  }

  Future<void> set(ThemeMode mode) async {
    if (mode == state) return;
    state = mode;
    await ref.read(sharedPreferencesProvider).setString(_prefsKey, _encode(mode));
  }

  /// Hızlı geçiş (tasarım önizlemesi / kısayol için): Açık ↔ Koyu.
  Future<void> toggle(Brightness current) =>
      set(current == Brightness.dark ? ThemeMode.light : ThemeMode.dark);

  static ThemeMode _decode(String? value) => switch (value) {
    'light' => ThemeMode.light,
    'dark' => ThemeMode.dark,
    _ => ThemeMode.system,
  };

  static String _encode(ThemeMode mode) => switch (mode) {
    ThemeMode.light => 'light',
    ThemeMode.dark => 'dark',
    ThemeMode.system => 'system',
  };
}

extension ThemeModeLabel on ThemeMode {
  /// Ayarlar ekranındaki Türkçe etiket.
  String get trLabel => switch (this) {
    ThemeMode.light => 'Açık',
    ThemeMode.dark => 'Koyu',
    ThemeMode.system => 'Sistem',
  };
}
