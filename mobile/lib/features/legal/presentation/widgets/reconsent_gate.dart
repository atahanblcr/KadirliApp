import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/app_routes.dart';
import '../../../auth/application/auth_controller.dart';
import '../../application/legal_providers.dart';
import '../../application/reconsent_prompt.dart';

/// Sekme kabuğunu saran **yeniden onay kapısı** (12.17).
///
/// Yönetici *esaslı* bir değişiklik yayınladığında (`requiresReconsent`)
/// kullanıcıyı **bir kez** `/yasal-onay` ekranına götürür.
///
/// ## Neden burada, neden `redirect` içinde değil
/// `GoRouter.redirect` **eşzamanlı** çalışıyor; rıza durumu ise bir ağ
/// isteğinin sonucu. Redirect'e taşımak ya her yönlendirmede `await` beklemek
/// (uygulamayı açılışta kilitler) ya da veriyi router'da önbelleğe almak
/// (ikinci bir sahip) demekti.
///
/// ## Neden "bir kez"
/// Sunucu ölçütü her istekte aynı cevabı verir; ekran kapatılabildiği için
/// (isteğe bağlı belgede) her sekme değişiminde yeniden açılırsa kullanıcı
/// uygulamayı kullanamaz hâle gelir. İşaret **kullanıcı başına** tutuluyor:
/// başka bir hesapla giriş yapıldığında kapı yeniden çalışır — yoksa ikinci
/// kullanıcının bekleyen onayı sessizce hiç sorulmazdı.
class ReconsentGate extends ConsumerStatefulWidget {
  const ReconsentGate({super.key, required this.child});

  final Widget child;

  @override
  ConsumerState<ReconsentGate> createState() => _ReconsentGateState();
}

class _ReconsentGateState extends ConsumerState<ReconsentGate> {
  @override
  Widget build(BuildContext context) {
    final user = ref.watch(currentUserProvider);
    final pending = ref.watch(pendingReconsentsProvider);

    if (user != null && pending.isNotEmpty) {
      final prompts = ref.read(reconsentPromptProvider.notifier);
      if (!prompts.wasPrompted(user.id)) {
        // ⚠️ `addPostFrameCallback` içinde (checklist §5): build sırasında
        // gezinmek ekranı router redirect'inin üstünde asılı bırakır.
        // ⚠️ `context.push` güvenli: `/yasal-onay` bir **kabuk rotası değil**
        // (§7 kod-dışı — kabuk rotalarına `AppNav` gerekir).
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (!mounted) return;
          if (GoRouterState.of(context).matchedLocation == AppRoutes.reconsent) {
            return;
          }
          prompts.markPrompted(user.id);
          context.push(AppRoutes.reconsent);
        });
      }
    }

    return widget.child;
  }
}
