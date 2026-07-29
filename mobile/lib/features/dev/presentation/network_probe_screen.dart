import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/env.dart';
import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';

/// **Ağ tanılama** (yalnız debug) — Faz 11.2'nin canlı doğrulama aracı.
///
/// Gerçek API'ye karşı zarf açma, sayfalama, hata eşleme ve announcements
/// quirk'ini tek ekranda kanıtlar. Modül ekranları geldikçe de "API ayakta mı,
/// base URL doğru mu" sorusunun en hızlı cevabı burasıdır.
class NetworkProbeScreen extends ConsumerStatefulWidget {
  const NetworkProbeScreen({super.key});

  @override
  ConsumerState<NetworkProbeScreen> createState() => _NetworkProbeScreenState();
}

class _NetworkProbeScreenState extends ConsumerState<NetworkProbeScreen> {
  final Map<String, _ProbeResult> _results = {};
  bool _running = false;

  late final List<_Probe> _probes = [
    _Probe(
      title: 'Mahalleler',
      subtitle: 'GET /v1/neighborhoods — düz liste, zarf açılır',
      run: (api) async {
        final names = await api.getList('/v1/neighborhoods', (json) => json['name'] as String);
        return '${names.length} mahalle · ilk: ${names.isEmpty ? '—' : names.first}';
      },
    ),
    _Probe(
      title: 'Duyurular (sayfalı)',
      subtitle: 'GET /v1/announcements?page=1&limit=3 — PagedResult',
      run: (api) async {
        final page = await api.getPaged(
          '/v1/announcements',
          (json) => json['title'] as String? ?? '(başlıksız)',
          page: 1,
          limit: 3,
        );
        return 'sayfa ${page.currentPage}/${page.totalPages} · '
            '${page.items.length}/${page.totalCount} kayıt · '
            'devamı ${page.hasNextPage ? 'var' : 'yok'}';
      },
    ),
    _Probe(
      title: 'Nöbetçi eczane',
      subtitle: 'GET /v1/pharmacies/on-duty — tarih + görsel yardımcıları',
      run: (api) async {
        final data = await api.get('/v1/pharmacies/on-duty');
        if (data is! List || data.isEmpty) return 'bugün için atama yok (boş liste)';
        final first = Map<String, dynamic>.from(data.first as Map);
        return '${first['pharmacyName'] ?? first['name'] ?? '—'} · '
            'bugün: ${AppDate.dayWithWeekday(DateTime.now())}';
      },
    ),
    _Probe(
      title: 'Duyuru quirk (200 + success:false)',
      subtitle: 'GET /v1/announcements/{yok} — NOT_FOUND bekleniyor',
      expectsError: true,
      run: (api) => api.get('/v1/announcements/00000000-0000-0000-0000-000000000000'),
    ),
    _Probe(
      title: 'Gerçek 404',
      subtitle: 'GET /v1/ads/{yok} — NOT_FOUND bekleniyor',
      expectsError: true,
      run: (api) => api.get('/v1/ads/00000000-0000-0000-0000-000000000000'),
    ),
    _Probe(
      title: 'Korumalı uç (token yok)',
      subtitle: 'GET /v1/users/me — UNAUTHORIZED bekleniyor',
      expectsError: true,
      run: (api) => api.get('/v1/users/me'),
    ),
    _Probe(
      title: 'Zarfsız uç',
      subtitle: 'GET /health — sarmalanmadan geçmeli',
      run: (api) async {
        final data = await api.get('/health');
        final status = data is Map ? data['status'] : data;
        return 'durum: $status';
      },
    ),
  ];

  @override
  void initState() {
    super.initState();
    // Tanılama ekranı açılır açılmaz koşar — amacı zaten "API ayakta mı?"
    // sorusuna tek dokunuşta cevap vermek.
    WidgetsBinding.instance.addPostFrameCallback((_) => _runAll());
  }

  Future<void> _runAll() async {
    setState(() {
      _running = true;
      _results.clear();
    });

    final api = ref.read(apiClientProvider);
    for (final probe in _probes) {
      final stopwatch = Stopwatch()..start();
      _ProbeResult result;
      try {
        final value = await probe.run(api);
        stopwatch.stop();
        result = _ProbeResult(
          ok: !probe.expectsError,
          detail: value is String ? value : 'başarılı',
          elapsed: stopwatch.elapsed,
        );
      } on ApiException catch (error) {
        stopwatch.stop();
        result = _ProbeResult(
          // Hata BEKLENEN sondaysa bu da "geçti" sayılır.
          ok: probe.expectsError,
          detail: '${error.code} — ${error.message}',
          traceId: error.traceId,
          elapsed: stopwatch.elapsed,
        );
      }
      if (!mounted) return;
      setState(() => _results[probe.title] = result);
    }

    if (mounted) setState(() => _running = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final passed = _results.values.where((r) => r.ok).length;

    return AppScaffold(
      title: 'Ağ tanılama',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.sm,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          AppCard(
            accentStripe: palette.info,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('API kökü', style: theme.textTheme.labelMedium?.copyWith(color: palette.muted)),
                AppSpacing.gapXs,
                SelectableText(Env.apiBaseUrl, style: theme.textTheme.bodyMedium),
                AppSpacing.gapLg,
                AppButton(
                  label: _results.isEmpty ? 'Testleri çalıştır' : 'Yeniden çalıştır',
                  icon: Icons.play_arrow_rounded,
                  expand: true,
                  loading: _running,
                  onPressed: _running ? null : _runAll,
                ),
                if (_results.isNotEmpty) ...[
                  AppSpacing.gapMd,
                  Text(
                    '$passed/${_probes.length} test beklendiği gibi sonuçlandı',
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: passed == _probes.length ? palette.success : palette.warning,
                    ),
                  ),
                ],
              ],
            ),
          ),
          AppSpacing.gapXl,
          const SectionHeader(title: 'Uçlar'),
          for (final probe in _probes) ...[
            _ProbeTile(probe: probe, result: _results[probe.title]),
            AppSpacing.gapMd,
          ],
        ],
      ),
    );
  }
}

class _Probe {
  const _Probe({
    required this.title,
    required this.subtitle,
    required this.run,
    this.expectsError = false,
  });

  final String title;
  final String subtitle;
  final Future<dynamic> Function(ApiClient api) run;

  /// Bu uçtan hata beklenir (hata eşlemesinin kanıtı) — hata gelmezse test kalır.
  final bool expectsError;
}

class _ProbeResult {
  const _ProbeResult({
    required this.ok,
    required this.detail,
    required this.elapsed,
    this.traceId,
  });

  final bool ok;
  final String detail;
  final String? traceId;
  final Duration elapsed;
}

class _ProbeTile extends StatelessWidget {
  const _ProbeTile({required this.probe, this.result});

  final _Probe probe;
  final _ProbeResult? result;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final state = result;

    final (IconData icon, Color color) = switch (state) {
      null => (Icons.radio_button_unchecked_rounded, palette.muted),
      _ when state.ok => (Icons.check_circle_rounded, palette.success),
      _ => (Icons.error_rounded, palette.danger),
    };

    return AppCard(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 20),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(child: Text(probe.title, style: theme.textTheme.titleSmall)),
                    if (state != null)
                      Text(
                        '${state.elapsed.inMilliseconds}ms',
                        style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
                      ),
                  ],
                ),
                AppSpacing.gapXs,
                Text(
                  probe.subtitle,
                  style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                ),
                if (state != null) ...[
                  AppSpacing.gapSm,
                  Text(state.detail, style: theme.textTheme.bodySmall?.copyWith(color: color)),
                  if (state.traceId != null)
                    Text(
                      'trace: ${state.traceId}',
                      style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
                    ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
