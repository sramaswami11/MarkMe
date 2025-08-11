# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything and restore
COPY . .
RUN dotnet restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Auto-detect the main DLL (in case project name changes)
CMD ["sh", "-c", "dotnet $(ls *.dll | grep -v Test | head -n 1)"]
