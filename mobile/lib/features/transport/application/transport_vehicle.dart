import 'package:flutter/material.dart';

/// Faz 12.6 — hattın araç tipi (`vehicleType`: `"bus"` | `"minibus"`).
///
/// Kadirli'de "Adana minibüsü" ile "Adana otobüsü" **ayrı işlerdir**: farklı
/// yerden kalkar, farklı sıklıkta gider. 12.5 bu ayrımı sunucuya yazdı, burada
/// vatandaşa gösteriliyor.
///
/// ⚠️ Ham değerler **kontrattır** (görünmez sözleşme #47) — sunucu metin
/// saklıyor, enum sırası değil. Türkçe karşılığı istemcide üretilir.
enum TransportVehicle {
  /// Süzgeç şeridindeki "Tümü" — uca **hiç parametre gönderilmez**.
  ///
  /// 🔑 Bu seçeneğin varlığı bilinçli: şerit yalnız Otobüs/Minibüs olsaydı,
  /// sunucuya yarın üçüncü bir tip eklendiğinde (dolmuş, servis) o hatlar
  /// mağazadaki eski sürümlerde **hiçbir süzgeçte görünmezdi** — liste hata
  /// vermeden eksik olurdu. Panelin şeridi de aynı üçlüyü kullanıyor.
  all(null, 'Tümü', Icons.list_rounded),

  bus('bus', 'Otobüs', Icons.directions_bus_rounded),

  minibus('minibus', 'Minibüs', Icons.airport_shuttle_rounded);

  const TransportVehicle(this.apiValue, this.label, this.icon);

  /// Uca giden `vehicleType` değeri; [all] için `null` (parametre yazılmaz).
  final String? apiValue;

  final String label;
  final IconData icon;

  /// Süzgeç şeridinin sırası.
  static const List<TransportVehicle> filters = [all, bus, minibus];

  /// Ham sunucu değerinden tip. **Tanınmayan değer `null` döner** — uydurma bir
  /// etiket basmaktansa rozeti hiç çizmemek doğru: sunucu yarın "dolmus"
  /// gönderirse kart "Otobüs" yazıp **yalan söylememeli**.
  static TransportVehicle? parse(String? raw) {
    final value = raw?.trim().toLowerCase();
    if (value == null || value.isEmpty) return null;
    for (final vehicle in filters) {
      if (vehicle.apiValue == value) return vehicle;
    }
    return null;
  }
}
