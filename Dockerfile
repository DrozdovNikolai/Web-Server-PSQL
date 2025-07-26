# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/PostgresControlPlane.Api/PostgresControlPlane.Api.csproj \
    src/PostgresControlPlane.Api/packages.lock.json \
    src/PostgresControlPlane.Api/
RUN dotnet restore src/PostgresControlPlane.Api/PostgresControlPlane.Api.csproj --locked-mode

COPY src/PostgresControlPlane.Api/ src/PostgresControlPlane.Api/
RUN dotnet publish src/PostgresControlPlane.Api/PostgresControlPlane.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app

RUN apk add --no-cache font-dejavu icu-libs tzdata \
    && mkdir -p /app/data \
    && chown -R "$APP_UID:$APP_UID" /app

COPY --from=build --chown=$APP_UID:$APP_UID /out/ .

USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget -q -O - http://127.0.0.1:8080/server/health/live >/dev/null || exit 1

ENTRYPOINT ["dotnet", "PostgresControlPlane.Api.dll"]
