# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/PayFlow.Shared/PayFlow.Shared.csproj src/PayFlow.Shared/
COPY PayFlow.PointsApi/PayFlow.PointsApi.csproj PayFlow.PointsApi/
RUN dotnet restore PayFlow.PointsApi/PayFlow.PointsApi.csproj

# Copy source
COPY src/PayFlow.Shared/ src/PayFlow.Shared/
COPY PayFlow.PointsApi/ PayFlow.PointsApi/
RUN dotnet publish PayFlow.PointsApi/PayFlow.PointsApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "PayFlow.PointsApi.dll"]
