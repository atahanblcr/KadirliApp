# syntax=docker/dockerfile:1
#
# Faz 12.21a — KadirliApp.Web (yönetim paneli) üretim imajı.
#
# ⚠️ Bağlam ÇÖZÜM KÖKÜDÜR:
#     docker build -f KadirliApp.Web/Dockerfile -t kadirliapp-web .
#
# 🔑 **İmajda `npm` adımı YOK ve bu bilinçli.** Panelin `wwwroot/css/panel.css`,
# `wwwroot/js/panel.js` ve `wwwroot/lib/*` varlıkları türetilmiş ama COMMİT
# EDİLMİŞ dosyalardır (12.9 kararı: depoyu klonlayan biri npm kurmadan paneli
# açabilmeli). Sürüklenmeyi CI denetler (`.github/workflows/dotnet.yml`), imaj
# değil — imaja bir npm adımı koymak, aynı çıktıyı ikinci bir yerde üretmek ve
# iki üreticinin ayrışmasına kapı açmak olurdu.
#
# 🔴 Bunun bedeli: `.dockerignore`'a "wwwroot" yazan biri paneli TAMAMEN stilsiz
# bırakır ve harita seçiciyi öldürür. İyi haber, hata SESSİZ DEĞİL: PanelAssetGuard
# Production'da varlık eksikse uygulamayı hiç açmaz (§7 madde 51). 12.21c'de
# bozma turuyla ölçüldü — konteyner gerçekten açılmadı.

# ── 1) Derleme ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY KadirliApp.Domain/KadirliApp.Domain.csproj                 KadirliApp.Domain/
COPY KadirliApp.Application/KadirliApp.Application.csproj       KadirliApp.Application/
COPY KadirliApp.Infrastructure/KadirliApp.Infrastructure.csproj KadirliApp.Infrastructure/
COPY KadirliApp.Web/KadirliApp.Web.csproj                       KadirliApp.Web/
RUN dotnet restore KadirliApp.Web/KadirliApp.Web.csproj

COPY . .
RUN dotnet publish KadirliApp.Web/KadirliApp.Web.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ── 2) Çalışma ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# HEALTHCHECK'in ihtiyacı (Api imajındaki notun aynısı).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Panel de uploads'ı okur (görsel önizlemeleri): Api ile AYNI volume'e bağlanır.
# ⚠️ İki host aynı klasörü paylaşıyor — ayrı volume verilirse panelde yüklenen
# görsel mobilde 404 olur ve tersi.
RUN mkdir -p /app/uploads && chown -R app:app /app/uploads

COPY --from=build --chown=app:app /app/publish .
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    FileStorage__UploadDirectory=/app/uploads

EXPOSE 8080

# Panelde /health/* uçları da var (MapInfrastructureHealthEndpoints, Api ile ortak).
# 🔑 12.20a'dan beri o uçlar `[AllowAnonymous]` taşımak ZORUNDA: panel fail-closed
# ve muafiyet olmasaydı probe 302 → /account/login alır, Docker bunu "unhealthy"
# sayar ve sebep hiçbir logda görünmezdi.
HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD ["/bin/sh", "-c", "curl -fsS http://localhost:8080/health/ready || exit 1"]

ENTRYPOINT ["dotnet", "KadirliApp.Web.dll"]
