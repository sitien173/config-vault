# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ConfigVault.sln ./
COPY src/ConfigVault.Api/ConfigVault.Api.csproj src/ConfigVault.Api/
COPY src/ConfigVault.Core/ConfigVault.Core.csproj src/ConfigVault.Core/
RUN dotnet restore src/ConfigVault.Api/ConfigVault.Api.csproj

COPY src/ src/
RUN dotnet publish src/ConfigVault.Api/ConfigVault.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConfigVault.Api.dll"]
