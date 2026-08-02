import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/paging/paged_feed.dart';
import '../data/complaints_repository.dart';
import '../data/models/complaint.dart';

/// Filtresiz akış: uçta hiçbir sorgu parametresi yok → filtre tipi **boş
/// kayıt** (`()`), ortak [PagedFeedController]'ı olduğu gibi kullanabilmek
/// için (boş kayıtlar birbirine eşittir, `applyFilter` no-op olur).
typedef MyComplaintsFeedState = PagedFeedState<Complaint, ()>;

/// "Bildirimlerim" listesi (`GET /v1/complaints/my` `[A]`).
///
/// Durum filtresi bilinçli olarak yok: uç desteklemiyor, istemcide süzmek
/// sayfalama ile tutarsız sonuç verirdi (11.11 mekan sıralaması kararı).
class MyComplaintsFeedController extends PagedFeedController<Complaint, ()> {
  @override
  () get initialFilter => ();

  @override
  Future<PagedResult<Complaint>> fetchPage({
    required int page,
    required int limit,
    required () filter,
  }) => ref.read(complaintsRepositoryProvider).mine(page: page, limit: limit);

  @override
  String idOf(Complaint item) => item.id;
}

final myComplaintsFeedProvider =
    NotifierProvider<MyComplaintsFeedController, MyComplaintsFeedState>(
      MyComplaintsFeedController.new,
    );
