# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY PayFlow.PointsApi/PayFlow.PointsApi.csproj PayFlow.PointsApi/
COPY src/PayFlow.Shared/PayFlow.Shared.csproj src/PayFlow.Shared/
COPY src/PayFlow.OrdersApi/PayFlow.OrdersApi.csproj src/PayFlow.OrdersApi/
COPY src/PayFlow.Host/PayFlow.Host.csproj src/PayFlow.Host/
RUN dotnet restore src/PayFlow.Host/PayFlow.Host.csproj

# Copy full source
COPY PayFlow.PointsApi/ PayFlow.PointsApi/
COPY src/PayFlow.Shared/ src/PayFlow.Shared/
COPY src/PayFlow.OrdersApi/ src/PayFlow.OrdersApi/
COPY src/PayFlow.Host/ src/PayFlow.Host/

RUN dotnet publish src/PayFlow.Host/PayFlow.Host.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "PayFlow.Host.dll"]
