import 'package:flutter/material.dart';

import '../../../../core/navigation/app_modules.dart';
import '../../../../core/router/app_nav.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';

/// Ana Sayfa modül ızgarası (MOBILE_UX_PLAN §5): 4 sütun, ikon + etiket.
///
/// Kaynak [kAppModules] — ızgaraya modül eklemek için burası değil, kayıt
/// listesi düzenlenir.
class ModuleGrid extends StatelessWidget {
  const ModuleGrid({super.key, this.modules = kAppModules});

  final List<AppModule> modules;

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      // Ana Sayfa'nın kendi ListView'ı kaydırıyor.
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      padding: EdgeInsets.zero,
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 4,
        mainAxisSpacing: AppSpacing.sm,
        crossAxisSpacing: AppSpacing.sm,
        // İkon + iki satıra sığabilen etiket (yazı ölçeği 1.4'e çıkabiliyor).
        childAspectRatio: 0.82,
      ),
      itemCount: modules.length,
      itemBuilder: (context, index) => _ModuleTile(module: modules[index]),
    );
  }
}

class _ModuleTile extends StatelessWidget {
  const _ModuleTile({required this.module});

  final AppModule module;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Semantics(
      button: true,
      label: '${module.label} modülü',
      child: Material(
        color: theme.colorScheme.surface,
        clipBehavior: Clip.antiAlias,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rMd,
          side: BorderSide(color: palette.border),
        ),
        child: InkWell(
          // ⚠️ Modül rotası aynı zamanda bir **sekme** ise (İlanlar) `push`
          // kabuğun üstüne ikinci bir kabuk yığar; doğru davranış o sekmeye
          // geçmektir. Diğer modüller kabuğun dışına açılır (11.4 kararı).
          //
          // 🔑 12.3: karar artık burada değil `AppNav`'da. Buradaki elle yazılmış
          // `AppRoutes.tabs` kontrolü DOĞRU sezgiye sahipti ama yalnız sekme
          // **köklerini** tanıyordu; `/ilanlar/:id` gibi sekme **alt** rotaları
          // (push bildiriminin deep-link hedefleri) kapsam dışındaydı ve
          // uygulamayı çökertebiliyordu. Kural tek yerde: `core/router/app_nav.dart`.
          onTap: () => AppNav.of(context, module.route),
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.xs,
              vertical: AppSpacing.sm,
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: theme.colorScheme.primaryContainer,
                    borderRadius: AppRadius.rSm,
                  ),
                  child: Icon(
                    module.icon,
                    size: 21,
                    color: theme.colorScheme.onPrimaryContainer,
                  ),
                ),
                AppSpacing.gapSm,
                Flexible(
                  child: Text(
                    module.label,
                    style: theme.textTheme.labelMedium,
                    textAlign: TextAlign.center,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
