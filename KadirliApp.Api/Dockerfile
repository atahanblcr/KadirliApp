# syntax=docker/dockerfile:1
#
# Faz 12.21a — KadirliApp.Api üretim imajı.
#
# ⚠️ Bağlam ÇÖZÜM KÖKÜDÜR, bu klasör değil:
#     docker build -f KadirliApp.Api/Dockerfile -t kadirliapp-api .
# Sebep: proje referansları (Application/Domain/Infrastructure) kardeş klasörlerde;
# bağlam bu klasör olsaydı restore ilk satırda düşerdi.

# ── 1) Derleme ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce YALNIZ proje dosyaları: kaynak değişince restore katmanı önbellekten gelsin.
# (KadirliApp.Tests bilinçli olarak yok — .dockerignore onu zaten dışlıyor ve
#  üretim imajı test projesini restore etmemeli.)
COPY KadirliApp.Domain/KadirliApp.Domain.csproj                 KadirliApp.Domain/
COPY KadirliApp.Application/KadirliApp.Application.csproj       KadirliApp.Application/
COPY KadirliApp.Infrastructure/KadirliApp.Infrastructure.csproj KadirliApp.Infrastructure/
COPY KadirliApp.Api/KadirliApp.Api.csproj                       KadirliApp.Api/
RUN dotnet restore KadirliApp.Api/KadirliApp.Api.csproj

COPY . .
RUN dotnet publish KadirliApp.Api/KadirliApp.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── 2) Çalışma ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ⚠️ `aspnet:8.0` içinde curl/wget YOKTUR — HEALTHCHECK'in çalıştıracağı bir şey
# olmadan Docker komutu "unhealthy" sayar ve konteyner sonsuza kadar sağlıksız
# görünür (uygulama gayet iyi çalışırken). Ölçüldü, tahmin edilmedi.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# 🔴 NON-ROOT. `aspnet:8.0` imajı `app` (uid 1654) kullanıcısını hazır getiriyor.
# Neden önemli: konteyner root koşarsa, bağlanan uploads volume'üne root olarak
# yazar ve host tarafında dosyalar erişilemez sahiplikte kalır; ayrıca bir RCE
# doğrudan root'a çıkar.
# ⚠️ uploads dizini ÖNCEDEN açılıp sahipliği veriliyor: volume boş bir dizine
# bağlandığında Docker sahipliği imajdaki dizinden devralır. Bu satır olmadan
# non-root süreç kendi yükleme klasörüne yazamaz ve hata ancak İLK görsel
# yüklemesinde görünür — yani dağıtımdan saatler sonra.
RUN mkdir -p /app/uploads && chown -R app:app /app/uploads

COPY --from=build --chown=app:app /app/publish .
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    FileStorage__UploadDirectory=/app/uploads

EXPOSE 8080

# 🔑 /health/ready (live DEĞİL): "süreç ayakta mı" değil "trafik alabilir mi" sorusu.
# `live` bilerek `Predicate = _ => false` — Postgres çökmüşken de 200 döner, yani
# HEALTHCHECK'e bağlansaydı orkestratör bozuk bir konteynere trafik göndermeye
# devam ederdi (Infrastructure/Health/HealthEndpoints.cs).
# ⚠️ start-period uzun: açılışta EF migration + seed koşuyor.
HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD ["/bin/sh", "-c", "curl -fsS http://localhost:8080/health/ready || exit 1"]

ENTRYPOINT ["dotnet", "KadirliApp.Api.dll"]
