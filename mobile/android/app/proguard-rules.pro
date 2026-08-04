# Kadirli — yayın yapısı küçültme kuralları (Faz 11.16)
#
# Flutter eklentileri kendi `consumer-rules.pro` dosyalarını getiriyor, bu yüzden
# burada yalnız onların kapsamadığı durumlar var.

# Flutter motoru ve gömülü sınıflar (yansımayla çağrılıyor).
-keep class io.flutter.** { *; }
-keep class io.flutter.plugins.** { *; }

# Firebase Cloud Messaging — arka plan servisi ve alıcı manifestten adla
# çözülüyor; küçültücü "kullanılmıyor" sanıp atarsa push SESSİZCE çalışmaz
# (ne çökme olur ne log; bildirim hiç gelmez).
-keep class com.google.firebase.** { *; }

# Küçültme uyarılarını hataya çevirmiyoruz: eklentilerin isteğe bağlı
# bağımlılıkları (ör. Play Core) yoksa R8 uyarı basar, yapı kırılmamalı.
-dontwarn io.flutter.embedding.**
