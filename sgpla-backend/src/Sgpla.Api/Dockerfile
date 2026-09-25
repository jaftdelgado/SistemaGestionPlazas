# syntax=docker/dockerfile:1
# Contexto de build: sgpla-backend/
#   docker build -f src/Sgpla.Api/Dockerfile -t sgpla-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Sgpla.Api/Sgpla.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sgpla.Api.dll"]
