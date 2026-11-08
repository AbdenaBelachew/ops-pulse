# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpsPulse.Agent/OpsPulse.Agent.csproj OpsPulse.Agent/
RUN dotnet restore OpsPulse.Agent/OpsPulse.Agent.csproj

COPY OpsPulse.Agent/ OpsPulse.Agent/
RUN dotnet publish OpsPulse.Agent/OpsPulse.Agent.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Docker host socket (optional, for the future docker-monitoring agent mode)
ENTRYPOINT ["dotnet", "OpsPulse.Agent.dll"]
