# Run a local LiveKit server for FSEdu dev testing.
# Requires: Docker Desktop running on Windows.
#
# Usage (from project root, in PowerShell):
#   .\deploy\start-livekit-local.ps1
#
# Stops with Ctrl+C. The container is removed automatically.

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $repoRoot "deploy\livekit-dev.yaml"

if (-not (Test-Path $configPath)) {
    Write-Error "livekit-dev.yaml not found at $configPath"
    exit 1
}

Write-Host "🚀 Starting LiveKit dev server on ws://localhost:7880" -ForegroundColor Cyan
Write-Host "   API key:    devkey" -ForegroundColor DarkGray
Write-Host "   API secret: devsecret_must_be_at_least_32_characters_xxxxxxxxxx" -ForegroundColor DarkGray
Write-Host "   Config:     $configPath" -ForegroundColor DarkGray
Write-Host ""
Write-Host "💡 Make sure FSEdu.Api is also running (http://localhost:5080)" -ForegroundColor Yellow
Write-Host "   Then open the Web app, log in, and join a live session." -ForegroundColor Yellow
Write-Host ""

docker run --rm `
    --name fsedu-livekit-dev `
    -p 7880:7880 `
    -p 7881:7881 `
    -p 50000-50100:50000-50100/udp `
    -v "${configPath}:/etc/livekit.yaml:ro" `
    livekit/livekit-server:latest --config /etc/livekit.yaml
