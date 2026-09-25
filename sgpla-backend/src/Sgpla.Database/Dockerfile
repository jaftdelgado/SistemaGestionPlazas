# syntax=docker/dockerfile:1
# Contexto de build: sgpla-backend/
#   docker build -f src/Sgpla.Database/Dockerfile -t sgpla-migraciones .
# La cadena de conexión se recibe en la variable SGPLA_DB_CONNECTION.

FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Sgpla.Database/Sgpla.Database.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Sgpla.Database.dll"]
