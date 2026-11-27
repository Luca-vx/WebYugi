# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WebYugi/WebYugi.csproj WebYugi/
RUN dotnet restore WebYugi/WebYugi.csproj

COPY WebYugi/ WebYugi/
RUN dotnet publish WebYugi/WebYugi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render provides PORT; fall back to 10000 for local container runs.
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet WebYugi.dll --urls http://0.0.0.0:${PORT:-10000}"]
