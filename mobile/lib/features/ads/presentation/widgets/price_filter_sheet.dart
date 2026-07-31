import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';

/// Fiyat aralığı seçimi sonucu.
class PriceRangeResult {
  const PriceRangeResult({this.min, this.max});

  final num? min;
  final num? max;
}

/// Fiyat aralığı sayfası (alt sayfa).
///
/// ⭐ **Plandışı:** 11.8 planı yalnız sıralama/arama/kategori diyordu, ama uç
/// `minPrice`/`maxPrice` parametrelerini baştan destekliyor ve pazaryerinde
/// "50 bine kadar telefon" en sık kurulan filtre. Panelde de aynı filtre var
/// (10.9), yani mobil eksik kalmış oluyordu.
Future<PriceRangeResult?> showPriceFilterSheet(
  BuildContext context, {
  num? min,
  num? max,
}) => showModalBottomSheet<PriceRangeResult>(
  context: context,
  showDragHandle: true,
  isScrollControlled: true,
  builder: (context) => _PriceFilterSheet(min: min, max: max),
);

class _PriceFilterSheet extends StatefulWidget {
  const _PriceFilterSheet({this.min, this.max});

  final num? min;
  final num? max;

  @override
  State<_PriceFilterSheet> createState() => _PriceFilterSheetState();
}

class _PriceFilterSheetState extends State<_PriceFilterSheet> {
  late final _minController = TextEditingController(
    text: widget.min == null ? '' : _plain(widget.min!),
  );
  late final _maxController = TextEditingController(
    text: widget.max == null ? '' : _plain(widget.max!),
  );

  static String _plain(num value) =>
      value == value.roundToDouble() ? value.toInt().toString() : '$value';

  @override
  void dispose() {
    _minController.dispose();
    _maxController.dispose();
    super.dispose();
  }

  void _apply() => Navigator.of(context).pop(
    PriceRangeResult(
      min: AppMoney.parse(_minController.text),
      max: AppMoney.parse(_maxController.text),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(
        AppSpacing.xl,
        0,
        AppSpacing.xl,
        AppSpacing.xl + bottomInset,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Fiyat aralığı', style: theme.textTheme.titleMedium),
          AppSpacing.gapLg,
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: AppTextField(
                  label: 'En az',
                  hint: '0',
                  controller: _minController,
                  keyboardType: TextInputType.number,
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[\d.,]')),
                  ],
                  suffix: const _CurrencySuffix(),
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: AppTextField(
                  label: 'En çok',
                  hint: 'Sınırsız',
                  controller: _maxController,
                  keyboardType: TextInputType.number,
                  textInputAction: TextInputAction.done,
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[\d.,]')),
                  ],
                  onSubmitted: (_) => _apply(),
                  suffix: const _CurrencySuffix(),
                ),
              ),
            ],
          ),
          AppSpacing.gapXl,
          AppButton(label: 'Uygula', expand: true, onPressed: _apply),
          AppSpacing.gapSm,
          AppButton.ghost(
            label: 'Aralığı kaldır',
            expand: true,
            onPressed: () =>
                Navigator.of(context).pop(const PriceRangeResult()),
          ),
        ],
      ),
    );
  }
}

class _CurrencySuffix extends StatelessWidget {
  const _CurrencySuffix();

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(right: AppSpacing.md),
    child: Text(AppMoney.symbol, style: Theme.of(context).textTheme.bodyLarge),
  );
}
