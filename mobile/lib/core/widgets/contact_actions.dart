import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../utils/utils.dart';
import 'app_button.dart';

/// Bir yerin/kurumun iletişim aksiyonları: **Ara · Yol tarifi · Web · E-posta**.
///
/// **Neden ortak bileşen:** eczane, rehber kaydı, (11.11) taksi durağı ve mekan
/// ekranları aynı dört aksiyonu gösteriyor. Tek yerde olması "telefon yoksa
/// buton çıkmasın" kuralının her ekranda aynı uygulanmasını da garanti ediyor
/// — MOBILE_UX_PLAN'ın "işlevsiz buton yok" şartı.
///
/// Veri yoksa ilgili buton **hiç çizilmez**; hiçbiri yoksa widget boş döner.
/// Dış uygulama açılamazsa (WhatsApp/harita kurulu değil) kullanıcıya kısa bir
/// bilgi şeridi gösterilir — sessiz başarısızlık yok.
class ContactActions extends StatelessWidget {
  const ContactActions({
    super.key,
    this.phone,
    this.latitude,
    this.longitude,
    this.mapLabel,
    this.address,
    this.website,
    this.email,
    this.callLabel = 'Ara',
  });

  final String? phone;
  final double? latitude;
  final double? longitude;

  /// Harita uygulamasında görünecek etiket (yer adı).
  final String? mapLabel;

  /// Koordinat yoksa harita **adres metniyle** aranır.
  final String? address;

  final String? website;
  final String? email;

  /// "Ara" yerine "Eczaneyi ara" gibi bağlama özel etiket.
  final String callLabel;

  bool get _hasCoordinates => latitude != null && longitude != null;

  bool get _hasMap => _hasCoordinates || _clean(address) != null;

  @override
  Widget build(BuildContext context) {
    final phoneNumber = _clean(phone);
    final site = _clean(website);
    final mail = _clean(email);

    final buttons = <Widget>[
      if (phoneNumber != null)
        _Action(
          label: callLabel,
          icon: Icons.call_rounded,
          primary: true,
          onTap: () => AppLinks.call(phoneNumber),
          failureMessage: 'Arama başlatılamadı.',
        ),
      if (_hasMap)
        _Action(
          label: 'Yol tarifi',
          icon: Icons.directions_rounded,
          onTap: () => _hasCoordinates
              ? AppLinks.map(
                  latitude: latitude!,
                  longitude: longitude!,
                  label: mapLabel,
                )
              : AppLinks.mapSearch(_clean(address)!),
          failureMessage: 'Harita uygulaması açılamadı.',
        ),
      if (site != null)
        _Action(
          label: 'Web sitesi',
          icon: Icons.language_rounded,
          onTap: () => AppLinks.web(site),
          failureMessage: 'Bağlantı açılamadı.',
        ),
      if (mail != null)
        _Action(
          label: 'E-posta',
          icon: Icons.mail_outline_rounded,
          onTap: () => AppLinks.email(mail),
          failureMessage: 'E-posta uygulaması açılamadı.',
        ),
    ];

    if (buttons.isEmpty) return const SizedBox.shrink();

    // Wrap: 1-4 buton ekran genişliğine göre kendiliğinden sarılır (büyük yazı
    // ölçeğinde taşma olmaz).
    return Wrap(
      spacing: AppSpacing.sm,
      runSpacing: AppSpacing.sm,
      children: buttons,
    );
  }

  static String? _clean(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

class _Action extends StatelessWidget {
  const _Action({
    required this.label,
    required this.icon,
    required this.onTap,
    required this.failureMessage,
    this.primary = false,
  });

  final String label;
  final IconData icon;
  final Future<bool> Function() onTap;
  final String failureMessage;
  final bool primary;

  @override
  Widget build(BuildContext context) {
    final messenger = ScaffoldMessenger.of(context);

    Future<void> run() async {
      final opened = await onTap();
      if (!opened) {
        messenger.showSnackBar(SnackBar(content: Text(failureMessage)));
      }
    }

    return primary
        ? AppButton(label: label, icon: icon, onPressed: run)
        : AppButton.ghost(label: label, icon: icon, onPressed: run);
  }
}

/// Etiket + değer satırı (adres, çalışma saatleri, eczacı…).
///
/// [onTap] verilirse satır dokunulabilir olur (telefonu aramak gibi).
class InfoRow extends StatelessWidget {
  const InfoRow({
    super.key,
    required this.icon,
    required this.label,
    required this.value,
    this.onTap,
  });

  final IconData icon;
  final String label;
  final String value;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final content = Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: theme.colorScheme.primary),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
                ),
                AppSpacing.gapXs,
                Text(value, style: theme.textTheme.bodyLarge),
              ],
            ),
          ),
          if (onTap != null)
            Icon(Icons.chevron_right_rounded, color: palette.muted),
        ],
      ),
    );

    if (onTap == null) return content;
    return InkWell(onTap: onTap, borderRadius: AppRadius.rSm, child: content);
  }
}
