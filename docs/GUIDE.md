# OpsPulse — Configuration & Testing Guide

Step-by-step instructions to configure, run and verify every part of the
platform. Follow the phases in order; each ends with an explicit "✅ Verify"
checklist you can perform in the UI.

---

## Phase 0 — Prerequisites

| Tool | Minimum version | Check |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Node.js | 20.19 / 22+ | `node --version` |
| Docker Desktop | with Compose v2 | `docker compose version` |
| Git | any recent | `git --version` |

> **Port note:** OpsPulse intentionally uses non-default host ports —
> **PostgreSQL 5544**, **Redis 6399**, API **5000**, frontend **5173** — so it
> never clashes with locally installed services.

## Phase 1 — Infrastructure (PostgreSQL + Redis)

```bash
cd opspulse
cp .env.example .env          # dev credentials are pre-filled
docker compose up -d
docker compose ps             # wait for both to show (healthy)
```

✅ **Verify:** `docker exec opspulse-postgres psql -U opspulse -d opspulse -c "select 1;"` returns 1.

## Phase 2 — Database schema

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef -- database update \
  --project OpsPulse.Infrastructure --startup-project OpsPulse.API
```

✅ **Verify:** 28 tables exist:
`docker exec opspulse-postgres psql -U opspulse -d opspulse -c "\dt"`.

On first API start the seeder creates the 5 roles and the SuperAdmin account
from `Seed:SuperAdmin` (dev defaults: `admin@opspulse.dev` / `Admin#12345`).

## Phase 3 — Run the platform

Open three terminals (all from `backend/`):

```bash
# Terminal 1 — REST API + SignalR hub + Prometheus metrics
cd OpsPulse.API && dotnet run
#    → http://localhost:5000  (Scalar API docs: /scalar, metrics: /metrics)

# Terminal 2 — monitoring engine (5 schedulers)
cd OpsPulse.Worker && dotnet run
#    → service checks every 10s, Docker sync 30s, DB checks 60s, K8s sync 60s,
#      retention every 6h; look for "scheduler started" lines

# Terminal 3 — frontend
cd ../frontend/opspulse-web && npm install && npm run dev
#    → http://localhost:5173
```

✅ **Verify:**
- `http://localhost:5000/health` → `"status":"Healthy"` with postgresql + redis entries.
- Frontend loads; the login page shows the seed credentials hint.

## Phase 4 — Accounts & organizations

Two account models:

| Account | Role | Purpose |
|---|---|---|
| `admin@opspulse.dev` / `Admin#12345` | SuperAdmin | Platform oversight: sees all organizations/users, manages users everywhere. **Cannot** create org-scoped resources (services, databases…). |
| Registered accounts | OrganizationAdmin | Own an organization; create and manage all monitoring resources for it. |

**Steps:**
1. Open `http://localhost:5173/register`.
2. Fill: organization name, your name, email, password
   (min 10 chars with upper + lower + digit + special, e.g. `Str0ng!Passw0rd`).
3. Submit — you are signed in immediately as the org's admin.

✅ **Verify:** Dashboard shows your organization name; sidebar has all modules.

**User management (as SuperAdmin or OrgAdmin):** Users → *Create user* → pick
role (`DevOpsEngineer` = full monitoring config, `Operator` = operate +
acknowledge, `Viewer` = read-only). Toggle a user's shield icon to
deactivate — their sessions are revoked instantly.

## Phase 5 — Application monitoring (HTTP checks)

1. **Applications** → *Add application*.
2. Fill: Name (`OpsPulse API`), URL (`http://localhost:5000/api/v1/health`),
   environment, check interval (minimum 10s), timeout.
3. Within one scheduler tick (≤10s) the first check runs.

✅ **Verify:**
- Card shows **Up** with a response time; "check now" (activity icon) returns a toast with status + latency.
- Open the service → details page has a live response-time chart and history (auto-refreshes).
- Point a service at a dead URL (e.g. `http://localhost:59999`) → status **Down**, consecutive failures count up.

> **SSRF note:** private/loopback targets are blocked by default in production
> (`Monitoring:AllowPrivateNetworks=false`). Development settings allow them.

## Phase 6 — Server monitoring agent

1. **Monitoring Agents** → *Create API key* → copy the key (shown once).
2. Start the agent on the machine to monitor:

```bash
cd backend/OpsPulse.Agent
OpsPulse__ApiUrl=http://localhost:5000 \
OpsPulse__ApiKey=<your-key> \
OpsPulse__IntervalSeconds=15 \
dotnet run
```

3. The agent registers its host automatically and streams CPU/RAM/disk every 15s.

✅ **Verify:**
- **Servers** page: host shows **Online** with live CPU/memory percentages.
- Click the server name → detail page with a streaming CPU + memory chart.
- Stop the agent with Ctrl+C → after ~90s the server flips **Offline** and an `AgentDisconnected` event fires.

## Phase 7 — Database monitoring

1. **Databases** → *Register database*.
2. Choose PostgreSQL or SQL Server; enter host/port/database/user/password.
   The password is AES-GCM encrypted before storage (key: `Encryption:Key`).
3. First check runs within 60s.

✅ **Verify:** card turns **Healthy**; create a connection to the platform's
own database (`localhost:5544/opspulse`) to see real numbers (active
connections, size in MB, probe latency).

## Phase 8 — Docker & Kubernetes

**Docker** (works out of the box with Docker Desktop):
1. **Docker** → *Add Docker host* → endpoint `npipe://./pipe/docker_engine`
   (Windows) or `unix:///var/run/docker.sock` (Linux/macOS).
2. Sync is instant; the worker refreshes every 30s.

✅ **Verify:** host card shows `N running / M containers`; click it for the
container table with CPU% and memory.

**Kubernetes:**
1. **Kubernetes** → *Add cluster* → name + optional kubeconfig YAML
   (leave empty to use `~/.kube/config`, or paste a least-privilege kubeconfig).
2. If the cluster is unreachable it is marked so with the error — nothing breaks.

✅ **Verify:** cluster card shows nodes/pods/failed-pods counts; the nodes and
pods tables populate after sync.

## Phase 9 — Alerts, incidents, notifications

**Create a rule:** **Alert Rules** → *Create rule* →
- Metric: `Service unavailable` (immediate) or `CPU usage (%) > 85` etc.
- Sustained minutes: how long the breach must hold (0 = immediately).
- Cooldown minutes: quiet period after resolution before re-alerting.
- Scope: all services or one specific service.

**Test the full chain:**
1. Add a service pointing at a dead URL (Phase 5).
2. Create rule: metric `Service unavailable`, severity `Critical`.
3. Within ~30s: an **incident appears automatically** (deduplicated — one per rule+source).
4. A notification badge appears on the header bell; **Notifications** lists it.
5. Open the incident → timeline shows creation; **Acknowledge**, comment, **Resolve**.
6. Fix the service URL → next healthy check **auto-resolves** any open incident for that rule.

✅ **Verify:** incident row updates to Resolved; bell badge clears after "mark all read".

**Channels:** Alert Rules → *Add channel* → Email (needs `Smtp:Host` config) or
Webhook (POSTs incident JSON to any URL). Delivery status is tracked per
notification.

## Phase 10 — Reports & audit

- **Reports:** uptime % per service (24h/7d/30d), incident summary with avg
  resolution time, server utilization averages/peaks, **CSV export** button.
- **Audit Logs:** every login, monitoring change, user change and incident
  action with actor, IP and timestamp. Filter and page through.
- **System Settings:** runtime version, environment, live dependency health.

✅ **Verify:** Reports shows uptime numbers matching your service history;
Audit Logs contains the actions you just performed.

## Phase 11 — Real-time (SignalR) verification

1. Open the dashboard in two browser windows.
2. In window 1, watch the dashboard; in window 2, use "check now" on a service or wait for a status change.
3. Window 1 updates **without a manual reload** (`ServiceStatusChanged` over SignalR).

✅ **Verify:** header bell badge increments live when an alert fires.

## Phase 12 — Production deployment

**Docker Compose (full stack):**

```bash
cp .env.example .env
# Add to .env — REQUIRED for the full profile:
#   JWT_KEY=<64+ random chars>            openssl rand -base64 64
#   ENCRYPTION_KEY=<64 hex chars>         openssl rand -hex 32
#   SEED_ADMIN_PASSWORD=<strong password>
docker compose --profile full up -d --build
# frontend http://localhost:8090 · api http://localhost:8080
```

**Kubernetes:**

```bash
kubectl apply -f infrastructure/kubernetes/00-namespace-config.yaml
# create the real secret (see comments in the manifest):
kubectl -n opspulse create secret generic opspulse-secrets \
  --from-literal=ConnectionStrings__DefaultConnection='Host=postgres;...' \
  --from-literal=Jwt__Key='<64+ chars>' \
  --from-literal=Encryption__Key='<64 hex chars>' \
  --from-literal=Seed__SuperAdmin__Password='<strong password>'
kubectl apply -f infrastructure/kubernetes/10-deployments.yaml
```

✅ **Verify (production):**
- `GET /health` returns Healthy through the ingress.
- `GET /metrics` serves Prometheus text; wire `infrastructure/prometheus/prometheus.yml` into your Prometheus.
- Login works; register an organization; run through Phases 5-9 with
  `Monitoring:AllowPrivateNetworks=false` (default — real SSRF protection on).

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| API exits: `Encryption:Key must be…` | Missing 64-hex encryption key | Set `Encryption__Key` env var (`openssl rand -hex 32`) |
| API exits: `Jwt:Key` errors | Missing JWT signing key | Set `Jwt__Key` (dev default exists in appsettings.Development.json) |
| Worker exits at startup | DB/Redis not healthy yet | `docker compose ps` — wait for healthy, restart worker |
| Service checks never run | Worker not running | Start `OpsPulse.Worker`; watch for "Service check scheduler started" |
| Agent reports nothing | Wrong/expired API key | Re-create key; check agent console for 401s |
| "role opspulse does not exist" | Another Postgres on 5432 captured traffic | OpsPulse uses port **5544** — don't change connection strings to 5432 |
| Docker host unreachable | Engine endpoint wrong | Windows: `npipe://./pipe/docker_engine`; Linux: `unix:///var/run/docker.sock`; ensure Docker Desktop is running |
| Cluster sync fails | kubeconfig / RBAC | Paste a kubeconfig with least-privilege ServiceAccount, or enable Docker Desktop k8s |
| Emails skipped | SMTP not configured | Set `Smtp__Host/Port/UserName/Password/FromEmail` — otherwise deliveries are marked `Skipped` |

## Security checklist (production)

- [ ] All secrets via environment/secret store; `.env` and dev appsettings never committed
- [ ] `Monitoring:AllowPrivateNetworks=false`
- [ ] Strong `Seed__SuperAdmin__Password`; rotate after first login
- [ ] CORS `Cors:AllowedOrigins` limited to your frontend origin
- [ ] HTTPS in front of the API (ingress + cert-manager, or a TLS proxy)
- [ ] Database credentials for monitored databases are read-only users
- [ ] Kubernetes ServiceAccount uses least-privilege RBAC (view pods/nodes only)
