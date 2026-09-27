# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached independently of source changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ShopSphere.Domain/ShopSphere.Domain.csproj src/ShopSphere.Domain/
COPY src/ShopSphere.Application/ShopSphere.Application.csproj src/ShopSphere.Application/
COPY src/ShopSphere.Infrastructure/ShopSphere.Infrastructure.csproj src/ShopSphere.Infrastructure/
COPY src/ShopSphere.Api/ShopSphere.Api.csproj src/ShopSphere.Api/
RUN dotnet restore src/ShopSphere.Api/ShopSphere.Api.csproj

COPY src/ src/
RUN dotnet publish src/ShopSphere.Api/ShopSphere.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- runtime ----------
# Chiseled images contain no shell or package manager and run as a non-root user by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ShopSphere.Api.dll"]
