/// `GET|PATCH /v1/users/me` yanıtının (MyProfileDto) gerçek gövdesi.
///
/// Alan adları ve varsayılanlar backend'deki `MyProfileDto` +
/// `NotificationPreferencesDto` ile birebir — model/kontrat ayrışması burada
/// yakalanır.
Map<String, dynamic> profileBody({
  String id = '11111111-1111-1111-1111-111111111111',
  String phone = '+905321110001',
  String? username = 'ahmetk',
  int? age = 30,
  String role = 'user',
  String? neighborhoodId = '22222222-2222-2222-2222-222222222222',
  String? neighborhoodName = 'Savrun',
  String? profilePhotoUrl,
  Map<String, bool>? notificationPreferences,
  String? usernameLastChangedAt,
  String? neighborhoodLastChangedAt,
  String createdAt = '2026-01-15T09:00:00.0000000Z',
}) => {
  'id': id,
  'phone': phone,
  'email': null,
  'username': username,
  'age': age,
  'role': role,
  'primaryNeighborhoodId': neighborhoodId,
  'primaryNeighborhoodName': neighborhoodName,
  'profilePhotoUrl': profilePhotoUrl,
  'notificationPreferences':
      notificationPreferences ??
      const {
        'announcements': true,
        'deaths': true,
        'pharmacy': true,
        'events': true,
        'ads': false,
        'campaigns': false,
      },
  'usernameLastChangedAt': usernameLastChangedAt,
  'neighborhoodLastChangedAt': neighborhoodLastChangedAt,
  'createdAt': createdAt,
};

/// `GET /v1/neighborhoods` — profil düzenleme ekranının mahalle listesi.
List<Map<String, dynamic>> neighborhoodsBody() => [
  {'id': '22222222-2222-2222-2222-222222222222', 'name': 'Savrun', 'type': 'mahalle'},
  {'id': '33333333-3333-3333-3333-333333333333', 'name': 'Cumhuriyet', 'type': 'mahalle'},
];
