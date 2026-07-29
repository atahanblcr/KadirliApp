import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/theme_controller.dart';

/// Açık / Koyu / Sistem seçimi. 11.5'te Ayarlar ekranında da kullanılır.
class ThemeModeSelector extends ConsumerWidget {
  const ThemeModeSelector({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final mode = ref.watch(themeModeProvider);

    return SegmentedButton<ThemeMode>(
      segments: const [
        ButtonSegment(
          value: ThemeMode.light,
          label: Text('Açık'),
          icon: Icon(Icons.light_mode_rounded),
        ),
        ButtonSegment(
          value: ThemeMode.dark,
          label: Text('Koyu'),
          icon: Icon(Icons.dark_mode_rounded),
        ),
        ButtonSegment(
          value: ThemeMode.system,
          label: Text('Sistem'),
          icon: Icon(Icons.brightness_auto_rounded),
        ),
      ],
      selected: {mode},
      showSelectedIcon: false,
      onSelectionChanged: (selection) =>
          ref.read(themeModeProvider.notifier).set(selection.first),
    );
  }
}
