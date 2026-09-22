# LiveKit — Local Dev Setup

Runs a real LiveKit server on your Windows machine so you can test the live video classroom end-to-end without deploying to Hetzner.

## Prerequisites

- **Docker Desktop** (running)
- **FSEdu API + Web** running locally (HTTP profile, not HTTPS — see note below)

## Start LiveKit

From the project root, in PowerShell:

```powershell
.\deploy\start-livekit-local.ps1
```

Leave that window open. To stop, press `Ctrl+C`.

The script runs `livekit/livekit-server:latest` with:
- `ws://localhost:7880` — WebSocket signaling
- `tcp/7881` — WebRTC TCP fallback
- `udp/50000-50100` — WebRTC media (UDP)
- API key: `devkey`
- API secret: `devsecret_must_be_at_least_32_characters_xxxxxxxxxx`

These match what's in [src/Presentation/FSEdu.Api/appsettings.Development.json](src/Presentation/FSEdu.Api/appsettings.Development.json).

## Run the apps

In two other terminals:

```powershell
dotnet run --project src/Presentation/FSEdu.Api/FSEdu.Api.csproj
dotnet run --project src/Presentation/FSEdu.Web/FSEdu.Web.csproj
```

Open http://localhost:5100 in **Chrome or Edge** (Firefox works too).

> **Use the HTTP profiles, not HTTPS.** The browser would block `ws://localhost:7880` from an `https://` page (mixed content). On `http://localhost:5100` everything works.

## Test scenario

1. Log in as the **Teacher**: `+20115155693` / `0115155693`
2. Open `/teacher/live` → you should see the seeded "حصة مباشرة الآن — الجبر التفاعلي" with status **Live**.
3. Click **↗ ادخل الغرفة** → the browser asks for camera + microphone permission → grant it.
4. You should see your own video in the LiveKit grid.
5. In a second browser tab/window (or another browser), log in as the **Student**: `+20115155691` / `0115155691`.
6. Go to `/student/live` and click the same session → student joins as viewer.
7. The teacher's video appears in the student's tab. The student can chat via SignalR sidebar, raise hand, etc.

## Test recording

In the teacher's room:
1. Click the ⏺️ button — this calls LiveKit Egress to start a room-composite recording.

> **Recording requires the LiveKit Egress service**, which is *not* started by `start-livekit-local.ps1` (it adds a Chrome inside Docker which is heavy and brittle on Windows Docker). For recording, deploy to Hetzner using [DEPLOYMENT.md](DEPLOYMENT.md). The button will return an error locally — that's expected.

The rest of the live experience (video, audio, chat, raise hand, participant list) works fully on local.

## Troubleshooting

| Symptom                                      | Fix                                                                                |
| -------------------------------------------- | ---------------------------------------------------------------------------------- |
| Browser console: `WebSocket connection to 'ws://localhost:7880/' failed` | LiveKit container not running. Re-run `start-livekit-local.ps1`.                   |
| `IDX10703: ... key length is zero`           | API didn't pick up `LiveKit:ApiSecret`. Restart the API process.                   |
| Video doesn't appear after join              | Camera permission denied. Click the camera icon in the address bar → Always allow. |
| Student tab shows "غير متصل" forever         | API not running on port 5080, or signal-R hub blocked.                             |
| "Cannot subscribe / no media flowing"        | UDP 50000-50100 blocked by Windows Firewall. Allow the LiveKit container.          |

## Cleanup

```powershell
docker stop fsedu-livekit-dev   # if running detached
docker rm   fsedu-livekit-dev   # if not auto-removed
```
