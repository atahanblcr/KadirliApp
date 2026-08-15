import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../data/models/legal_document.dart';

/// Tek bir rıza satırı: **ön işaretsiz** onay kutusu + "Oku" bağlantısı.
///
/// 🔴 **Ön işaretli kutu KVKK'da rıza sayılmaz.** Bu bileşen kendi başına bir
/// başlangıç değeri **üretmez** — [granted] her zaman dışarıdan gelir ve
/// `ConsentSelection.initial` onu boş bir kümeyle kurar. Varsayılanı `true`
/// yapan bir satır burada tek karakterle yazılabilirdi; testin kilitlediği şey
/// tam olarak budur.
///
/// ⚠️ "Oku" ayrı bir dokunma hedefi: satırın tamamı metni açsaydı kutuyu
/// işaretlemek isteyen kullanıcı metin ekranına düşerdi; satırın tamamı kutuyu
/// çevirseydi metni okumanın yolu **hiç olmazdı**.
class ConsentCheckTile extends StatelessWidget {
  const ConsentCheckTile({
    super.key,
    required this.document,
    required this.granted,
    required this.onChanged,
    required this.onRead,
    this.enabled = true,
  });

  final LegalDocument document;
  final bool granted;
  final ValueChanged<bool> onChanged;
  final VoidCallback onRead;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ⚠️ `Checkbox` varsayılan dokunma alanı 48dp'nin altında kalabiliyor;
          // `materialTapTargetSize` erişilebilirlik tavanını korur.
          Checkbox(
            value: granted,
            onChanged: enabled ? (value) => onChanged(value ?? false) : null,
            materialTapTargetSize: MaterialTapTargetSize.padded,
          ),
          AppSpacing.wGapXs,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppSpacing.gapSm,
                // 🔑 Metin `Expanded` içinde: 1.4 yazı ölçeğinde ve 360dp
                // genişlikte uzun bir belge adı satırı taşırırdı (projenin
                // yedi kez tekrarlamış `RenderFlex overflow` sınıfı).
                GestureDetector(
                  onTap: enabled ? () => onChanged(!granted) : null,
                  child: Text.rich(
                    TextSpan(
                      children: [
                        TextSpan(
                          text: document.consentLabel,
                          style: theme.textTheme.bodyMedium,
                        ),
                        if (document.isMandatory)
                          TextSpan(
                            text: ' *',
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: palette.danger,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
                AppSpacing.gapXs,
                Row(
                  children: [
                    // Ayrı dokunma hedefi (bkz. sınıf notu).
                    // 🐛 `Expanded` şart: içteki `Flexible` yalnız **kendi**
                    // satırında iş görüyor; `InkWell` esnek olmadığı sürece
                    // dıştaki satır onun doğal genişliğini istiyor ve uzun bir
                    // belge adı 360–400dp'de **taşırıyordu** (bu projenin yedi
                    // kez tekrarlamış `RenderFlex overflow` sınıfı — testin
                    // ilk koşusunda gerçekten kırmızıya döndü).
                    Expanded(
                      child: InkWell(
                        onTap: onRead,
                        borderRadius: AppRadius.rSm,
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                            vertical: AppSpacing.xs,
                            horizontal: AppSpacing.xs,
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.description_outlined,
                                size: 15,
                                color: theme.colorScheme.primary,
                              ),
                              AppSpacing.wGapXs,
                              Flexible(
                                child: Text(
                                  '${document.title} — oku',
                                  overflow: TextOverflow.ellipsis,
                                  style: theme.textTheme.bodySmall?.copyWith(
                                    color: theme.colorScheme.primary,
                                    decoration: TextDecoration.underline,
                                    decorationColor: theme.colorScheme.primary,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    Text(
                      'v${document.versionNumber}',
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: palette.muted,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
