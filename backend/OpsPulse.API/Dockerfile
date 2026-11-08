# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY OpsPulse.Domain/OpsPulse.Domain.csproj OpsPulse.Domain/
COPY OpsPulse.Application/OpsPulse.Application.csproj OpsPulse.Application/
COPY OpsPulse.Infrastructure/OpsPulse.Infrastructure.csproj OpsPulse.Infrastructure/
COPY OpsPulse.API/OpsPulse.API.csproj OpsPulse.API/
COPY OpsPulse.slnx ./
RUN dotnet restore OpsPulse.slnx

COPY OpsPulse.Domain/ OpsPulse.Domain/
COPY OpsPulse.Application/ OpsPulse.Application/
COPY OpsPulse.Infrastructure/ OpsPulse.Infrastructure/
COPY OpsPulse.API/ OpsPulse.API/
RUN dotnet publish OpsPulse.API/OpsPulse.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpsPulse.API.dll"]
