# Build context mora biti koren repozitorijuma (API referencira Application, Domain i Infrastructure):
#   docker build -f src/BidArena.Api/Dockerfile -t bidarena-api .
# (docker compose to radi automatski.)

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Prvo samo .csproj fajlovi, da se restore sloj kešira dok se zavisnosti ne promene.
COPY src/BidArena.Domain/BidArena.Domain.csproj src/BidArena.Domain/
COPY src/BidArena.Application/BidArena.Application.csproj src/BidArena.Application/
COPY src/BidArena.Infrastructure/BidArena.Infrastructure.csproj src/BidArena.Infrastructure/
COPY src/BidArena.Api/BidArena.Api.csproj src/BidArena.Api/
RUN dotnet restore src/BidArena.Api/BidArena.Api.csproj

COPY src/ src/
RUN dotnet publish src/BidArena.Api/BidArena.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Serilog file sink piše u /app/logs; aplikacija radi kao non-root korisnik.
# Slike predmeta se čuvaju u /app/uploads (u compose-u je to volume).
RUN mkdir -p /app/logs /app/uploads && chown -R $APP_UID /app/logs /app/uploads

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "BidArena.Api.dll"]
