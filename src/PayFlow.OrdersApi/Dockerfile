# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/PayFlow.Shared/PayFlow.Shared.csproj src/PayFlow.Shared/
COPY src/PayFlow.OrdersApi/PayFlow.OrdersApi.csproj src/PayFlow.OrdersApi/
RUN dotnet restore src/PayFlow.OrdersApi/PayFlow.OrdersApi.csproj

# Copy source
COPY src/PayFlow.Shared/ src/PayFlow.Shared/
COPY src/PayFlow.OrdersApi/ src/PayFlow.OrdersApi/
RUN dotnet publish src/PayFlow.OrdersApi/PayFlow.OrdersApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8081
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8081

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "PayFlow.OrdersApi.dll"]
