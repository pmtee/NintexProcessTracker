# ── STAGE 1: BUILD ──────────────────────────────────────────
# I use the full SDK to restore packages, build and publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# I copy the csproj first so Docker caches the restore layer
# dotnet restore only re-runs when csproj changes — faster builds
COPY ProcessTracker/ProcessTracker.csproj ProcessTracker/
RUN dotnet restore ProcessTracker/ProcessTracker.csproj

# I now copy all source files and publish a release build
COPY ProcessTracker/ ProcessTracker/
WORKDIR /src/ProcessTracker
RUN dotnet publish ProcessTracker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── STAGE 2: RUNTIME ────────────────────────────────────────
# I use the smaller runtime-only image — no SDK, no build tools
# This keeps the final image lean and secure
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# I create a persistent data directory for the SQLite database
RUN mkdir -p /app/data

# I create a non-root user — never run containers as root
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

# I copy only the published output from the build stage
COPY --from=build --chown=appuser:appgroup /app/publish .

# I expose port 8080
EXPOSE 8080

# I set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# I start the application
ENTRYPOINT ["dotnet", "ProcessTracker.dll"]
