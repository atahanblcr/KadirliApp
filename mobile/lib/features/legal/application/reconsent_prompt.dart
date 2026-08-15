import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Yeniden onay ekranının **bu oturumda kime gösterildiği** (12.17).
///
/// 🔑 İşaret **kullanıcı başına** tutuluyor, tek bir `bool` değil: başka bir
/// hesapla giriş yapıldığında kapı yeniden çalışmalı, yoksa ikinci kullanıcının
/// bekleyen onayı **sessizce hiç sorulmazdı** — ve belirtisi olmazdı.
///
/// ⚠️ Kalıcı **değil** (uygulama kapanınca sıfırlanır) ve bu bilinçli: ölçüt
/// sunucuda (`needsReconsent`), yani onaylanmamış bir metin bir sonraki açılışta
/// yeniden sorulmalı. Kalıcı saklansaydı "şimdi değil" diyen kullanıcıya bir
/// daha **hiç** sorulmazdı.
final reconsentPromptProvider =
    NotifierProvider<ReconsentPromptController, Set<String>>(
      ReconsentPromptController.new,
    );

class ReconsentPromptController extends Notifier<Set<String>> {
  @override
  Set<String> build() => const {};

  bool wasPrompted(String userId) => state.contains(userId);

  void markPrompted(String userId) => state = {...state, userId};
}
