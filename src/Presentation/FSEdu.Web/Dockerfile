# ─── Build Stage ──────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FSEdu.sln", "Directory.Build.props", "./"]
COPY ["src/", "src/"]

RUN dotnet restore "src/Presentation/FSEdu.Web/FSEdu.Web.csproj"
RUN dotnet publish "src/Presentation/FSEdu.Web/FSEdu.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─── Runtime Stage ────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN useradd -m -u 1000 fsedu && chown -R fsedu:fsedu /app
USER fsedu

EXPOSE 8080
COPY --from=build --chown=fsedu:fsedu /app/publish .

ENTRYPOINT ["dotnet", "FSEdu.Web.dll"]
