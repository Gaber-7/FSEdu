# ─── Build Stage ──────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + project files for layer caching
COPY ["FSEdu.sln", "Directory.Build.props", "./"]
COPY ["src/", "src/"]

# Restore + Publish
RUN dotnet restore "src/Presentation/FSEdu.Api/FSEdu.Api.csproj"
RUN dotnet publish "src/Presentation/FSEdu.Api/FSEdu.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─── Runtime Stage ────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1000 fsedu && chown -R fsedu:fsedu /app
USER fsedu

EXPOSE 8080

# Make uploads directory
RUN mkdir -p wwwroot/uploads/receipts

COPY --from=build --chown=fsedu:fsedu /app/publish .

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FSEdu.Api.dll"]
