# FSEdu — Production Deployment Guide

Single-server self-hosted deployment for **Hetzner CCX23** (or any equivalent Linux VM) with:

- ASP.NET Core 9 API + Blazor Web
- SQL Server 2022 Express
- LiveKit Server (open-source SFU) — live video classroom
- LiveKit Egress — Chrome-based recording, written to local disk
- Coturn — TURN/STUN for NAT traversal
- Caddy — automatic HTTPS via Let's Encrypt
- Redis — Egress job queue

Everything runs as Docker containers via `docker compose`.

---

## 1) Server Recommendation

| Resource | Hetzner CCX23                       |
| -------- | ----------------------------------- |
| CPU      | 4 dedicated AMD vCPU                |
| RAM      | 16 GB                               |
| Disk     | 160 GB NVMe (recordings grow here)  |
| Traffic  | 20 TB / month included              |
| Cost     | ~ €31 / month                       |

Suitable for ~1,000 students with several simultaneous live rooms (≤ 200 participants per room).

---

## 2) DNS

Point an `A` record (and optional `AAAA`) for your domain at the server's public IP:

```
fsedu.example.com   →   <SERVER_PUBLIC_IP>
```

Wait until `dig +short fsedu.example.com` resolves before proceeding (Caddy needs this to issue the certificate).

---

## 3) Install Docker

On Ubuntu 22.04 / 24.04:

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
docker compose version
```

---

## 4) Open Firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp        # HTTP (Caddy ACME)
sudo ufw allow 443/tcp       # HTTPS
sudo ufw allow 443/udp       # HTTP/3
sudo ufw allow 3478/tcp      # TURN
sudo ufw allow 3478/udp      # TURN
sudo ufw allow 5349/tcp      # TURN-TLS
sudo ufw allow 5349/udp      # TURN-TLS
sudo ufw allow 7881/tcp      # LiveKit RTC TCP fallback
sudo ufw allow 50000:60000/udp   # LiveKit WebRTC media
sudo ufw allow 49160:49200/udp   # Coturn relay range
sudo ufw enable
```

---

## 5) Clone the Repo

```bash
sudo mkdir -p /opt/fsedu
sudo chown $USER:$USER /opt/fsedu
cd /opt/fsedu
git clone https://github.com/FullScreen-Solutions/FSEdu.git .
```

---

## 6) Configure Secrets

Copy the env template and fill in real values:

```bash
cp deploy/.env.example .env
nano .env
```

Generate strong values:

```bash
# JWT signing key (48 random bytes, base64)
openssl rand -base64 48

# LiveKit API key — must start with "API", 12 hex bytes after
echo "API$(openssl rand -hex 12)"

# LiveKit API secret
openssl rand -hex 32

# Strong DB password (must include upper, digit, symbol)
openssl rand -base64 24
```

Set `DOMAIN=fsedu.example.com` and `LIVEKIT_URL=wss://fsedu.example.com/livekit` to match your domain.

⚠️ **Never commit `.env`.** Keep a copy in your password manager.

---

## 7) Build and Start

```bash
docker compose pull            # base images (mssql, livekit, caddy, redis, coturn)
docker compose build           # build api + web from source
docker compose up -d
docker compose ps
```

First start takes 1–2 minutes (SQL Server init, Caddy ACME challenge).
Tail logs:

```bash
docker compose logs -f api
docker compose logs -f caddy
docker compose logs -f livekit
```

---

## 8) Verify

| Check                      | Command                                                                                |
| -------------------------- | -------------------------------------------------------------------------------------- |
| Web app loads              | `curl -I https://fsedu.example.com`                                                    |
| API health                 | `curl https://fsedu.example.com/api/health` → `{"status":"healthy"}`                   |
| LiveKit WS reachable       | `curl -I https://fsedu.example.com/livekit/` (HTTP 426 Upgrade Required is correct)    |
| TURN responding            | `nc -vz -u <SERVER_IP> 3478`                                                           |
| TLS cert valid             | `openssl s_client -connect fsedu.example.com:443 -servername fsedu.example.com`        |

Then in a browser:

1. Go to `https://fsedu.example.com`
2. Log in (create the admin user via `IdentitySeeder` — it runs automatically on first start)
3. As a teacher, schedule a live session and click **Start**
4. As a student, join and confirm video appears

---

## 9) Recordings

LiveKit Egress writes finished recordings to the `recordings` Docker volume. The API container mounts that same volume at `/app/wwwroot/recordings` (read-only) and Caddy serves it under `https://fsedu.example.com/recordings/<file>.mp4`.

To start a recording for a session, call the LiveKit Egress API from the API process (TODO: add a `POST /api/v1/teacher/live/{id}/record` endpoint that wraps `RoomCompositeEgress`). For MVP, recording can be started manually via `livekit-cli`:

```bash
docker exec fsedu-livekit livekit-cli start-room-composite-egress \
  --api-key "$LIVEKIT_API_KEY" --api-secret "$LIVEKIT_API_SECRET" \
  --room <RoomId> --layout grid --output-file /recordings/<RoomId>.mp4
```

When recording stops, LiveKit fires `egress_ended` to `http://localhost:8080/api/v1/webhooks/livekit`. The webhook handler (`LiveClassroomEndpoints.cs`) matches the room to a `LiveSession` and calls `session.AttachRecording(url)` so the player URL appears in the lecture history.

---

## 10) Backups

Daily backup of `db-data`, `api-uploads`, `recordings`, and `.env`:

```bash
sudo tar -czf /backup/fsedu-$(date +%F).tar.gz \
  /var/lib/docker/volumes/fsedu_db-data/_data \
  /var/lib/docker/volumes/fsedu_api-uploads/_data \
  /var/lib/docker/volumes/fsedu_recordings/_data \
  /opt/fsedu/.env
```

Add to `cron` and rotate to off-site storage (Hetzner Storage Box, S3, etc.).

For SQL Server, also keep periodic logical backups:

```bash
docker exec fsedu-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_SA_PASSWORD" -C \
  -Q "BACKUP DATABASE FSEdu TO DISK='/var/opt/mssql/backup/fsedu.bak' WITH INIT, COMPRESSION"
```

---

## 11) Updates

```bash
cd /opt/fsedu
git pull
docker compose build api web
docker compose up -d
docker compose logs -f api | head -100
```

EF Core migrations run automatically on API startup via `EnsureIdentityDbAsync()` and the seeders (Development only — for Production, run `dotnet ef database update` from a sidecar or remove the dev-only branch in `Program.cs`).

---

## 12) Networking Notes (why the compose file looks unusual)

- `livekit`, `livekit-egress`, and `coturn` use `network_mode: host` because they need direct access to a wide UDP port range (50000–60000 + 49160–49200) which would be inefficient to expose through Docker's userland NAT.
- `api` and `redis` therefore expose their ports on `127.0.0.1` so that the host-network containers (LiveKit, Egress) can reach them without leaking the ports publicly.
- `caddy` (bridge) reaches LiveKit (host) via `host.docker.internal:7880`, enabled by the `extra_hosts: host-gateway` mapping.
- All inter-service traffic that *can* stay on the bridge (`api ↔ db`, `web ↔ api`) does, for isolation.

---

## 13) Troubleshooting

| Symptom                                            | Likely cause / fix                                                              |
| -------------------------------------------------- | ------------------------------------------------------------------------------- |
| Caddy keeps retrying ACME                          | DNS not propagated; ports 80/443 blocked.                                       |
| Browser shows "WebSocket connection failed"        | `/livekit/` proxy not reaching host. Check `extra_hosts` on caddy.              |
| Video doesn't appear, no errors in console         | Camera/mic permission denied. Browser must be on HTTPS.                         |
| Egress recording never finishes                    | Egress can't reach LiveKit at `ws://localhost:7880`. Verify both on host net.   |
| Webhook never fires after egress ends              | API port `127.0.0.1:8080` not exposed. Check compose `ports:` on api service.   |
| TURN not used (some students can't connect)        | UDP 3478 + relay range 49160–49200 blocked at firewall.                         |

---

## 14) Cost Reference

| Item                     | Monthly                |
| ------------------------ | ---------------------- |
| Hetzner CCX23            | ~ €31                  |
| Domain (.com, annual/12) | ~ $1                   |
| Storage Box for backups  | ~ €4 (1 TB)            |
| **Total**                | **~ €36 (~$40)**       |

20 TB egress bandwidth is included with CCX23 — sufficient for ~3,000 hours of 720p video streaming per month.
