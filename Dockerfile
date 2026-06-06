# MySociety Web API — .NET 8 (Linux)
# Build:  docker build -t mysociety-api .
# Run:    docker run --rm -p 8080:8080 \
#           -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;" \
#           -e Jwt__Key="your-secret-key-at-least-32-characters-long" mysociety-api

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/LogFiles/Application

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MySociety.sln ./
COPY src/Domain/MySociety.Domain.csproj src/Domain/
COPY src/Application/MySociety.Application.csproj src/Application/
COPY src/Infrastructure/MySociety.Infrastructure.csproj src/Infrastructure/
COPY src/Api/MySociety.Api.csproj src/Api/
RUN dotnet restore src/Api/MySociety.Api.csproj

COPY src/ src/
WORKDIR /src/src/Api
RUN dotnet publish MySociety.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["/app/docker-entrypoint.sh"]
