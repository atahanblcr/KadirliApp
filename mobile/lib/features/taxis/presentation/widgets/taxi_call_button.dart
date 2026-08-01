import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/widgets/widgets.dart';
import '../../../auth/presentation/widgets/login_required_sheet.dart';
import '../../application/taxis_providers.dart';
import '../../data/models/taxi_driver.dart';

/// "Ara" akışının **tek kapısı**: giriş kontrolü → `POST /drivers/{id}/call`
/// → çevirici → sonuç bildirimi.
///
/// Hem listedeki hem detaydaki buton bunu çağırır; iki ekranın davranışı
/// birbirinden ayrışamaz.
Future<void> requestTaxiCall(
  BuildContext context,
  WidgetRef ref,
  TaxiDriver driver,
) async {
  // Uç `[A]`; anonim kullanıcı router'la Giriş'e ATILMAZ, davet görür.
  if (!await ensureSignedIn(
    context,
    ref,
    reason:
        'Taksi çağırmak için giriş yapmanız gerekiyor. Telefon numaranızla '
        'saniyeler içinde giriş yapabilirsiniz.',
  )) {
    return;
  }
  if (!context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);
  final result = await ref.read(taxiCallProvider.notifier).call(driver);

  final message = result.message;
  if (message != null) {
    messenger
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
}

/// Sürücüyü arayan buton — işlem sürerken **yalnız o sürücünün** butonu döner.
class TaxiCallButton extends ConsumerWidget {
  const TaxiCallButton({
    super.key,
    required this.driver,
    this.label = 'Ara',
    this.size = AppButtonSize.normal,
    this.expand = false,
  });

  final TaxiDriver driver;
  final String label;
  final AppButtonSize size;
  final bool expand;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final callingId = ref.watch(taxiCallProvider);
    final isCalling = callingId == driver.id;
    // Başka bir çağrı sürerken bu buton pasif: iki çeviriciyi arka arkaya açmak
    // kullanıcıyı şaşırtır.
    final blocked = callingId != null && !isCalling;

    return AppButton(
      label: label,
      icon: Icons.call_rounded,
      size: size,
      expand: expand,
      loading: isCalling,
      onPressed: blocked ? null : () => requestTaxiCall(context, ref, driver),
    );
  }
}
