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
# libldap2: biblioteca nativa que necesita System.DirectoryServices.Protocols para el bind LDAP en Linux.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libldap2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /var/lib/sgpla/archivos && chown $APP_UID /var/lib/sgpla/archivos
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sgpla.Api.dll"]
