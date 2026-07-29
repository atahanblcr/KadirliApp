import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// `main()` içinde gerçek örnekle override edilir (bkz. `main.dart`).
/// Senkron okunabilmesi için açılışta bir kez `await` edilir — tema seçimi
/// ilk kareden itibaren doğru olur (yanıp sönme yok).
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('sharedPreferencesProvider override edilmeli'),
);

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
