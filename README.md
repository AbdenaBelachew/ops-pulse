# OpsPulse

**Enterprise Infrastructure & Application Monitoring Platform**

OpsPulse is a centralized monitoring platform for backend applications and APIs, server
health, PostgreSQL and SQL Server databases, Kubernetes clusters, Docker containers,
incidents and alerting — built as a professional-grade, production-oriented product.

![Status](https://img.shields.io/badge/milestones-12%20of%2012%20complete-22c55e)

## Technology Stack

| Layer | Technologies |
|---|---|
| Backend | ASP.NET Core Web API 10, C#, Clean Architecture, EF Core 10, PostgreSQL, ASP.NET Core Identity, JWT |
| Realtime & Jobs | SignalR (Redis backplane), background schedulers, Serilog, Prometheus metrics |
| Monitoring engines | SSRF-safe HTTP checks, PostgreSQL/SQL Server probes, Kubernetes API, Docker Engine |
| Frontend | React 19, TypeScript, Vite 8, Tailwind CSS 4, React Router 7, TanStack Query 5, Zustand, Recharts, SignalR client |
| DevOps | Docker, Docker Compose, Kubernetes manifests, Nginx, GitHub Actions (CI + GHCR release) |

## Architecture

Modular monolith with Clean Architecture dependency direction:
`Domain ← Application ← Infrastructure ← API / Worker`.

| Project | Responsibility |
|---|---|
| `OpsPulse.API` | Controllers, middleware, auth, SignalR hubs, Swagger/OpenAPI |
| `OpsPulse.Domain` | Entities, enums, business rules, domain events |
| `OpsPulse.Application` | DTOs, interfaces, services, use cases, validators |
| `OpsPulse.Infrastructure` | EF Core, PostgreSQL, Identity, Redis, integrations |
| `OpsPulse.Worker` | Monitoring scheduler, checks, alert evaluation |
| `OpsPulse.Agent` | Server metric collection agent (CPU, RAM, disk, network) |
| `OpsPulse.Tests` | Unit, integration and security tests |

Full development plan and per-milestone log: [`docs/PROGRESS.md`](docs/PROGRESS.md).

## Repository Structure

```text
OpsPulse/
├── backend/           # .NET 10 solution (OpsPulse.slnx)
├── frontend/
│   └── opspulse-web/  # React + TypeScript + Tailwind dashboard
├── infrastructure/    # docker / kubernetes / nginx / prometheus (per milestone)
├── docs/
├── docker-compose.yml
└── README.md
```

## Quickstart

### Prerequisites

- .NET SDK 10.x — `dotnet --version`
- Node.js 20.19+ / 22+ — `node --version`
- Docker Desktop — `docker compose version`

### 1. Start PostgreSQL and Redis

```bash
cp .env.example .env      # adjust if needed
docker compose up -d
docker compose ps         # wait for (healthy)
```

> **Note on ports:** OpsPulse binds **5544** (PostgreSQL) and **6399** (Redis) on the
> host instead of 5432/6379, so it does not clash with locally installed services.
> Container-side ports remain standard.

### 2. Apply the database schema

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef -- database update \
  --project OpsPulse.Infrastructure --startup-project OpsPulse.API
```

### 3. Run the API

```bash
cd backend/OpsPulse.API
dotnet run
# http://localhost:5000  — Swagger UI: http://localhost:5000/scalar
```

### 4. Run the frontend

```bash
cd frontend/opspulse-web
npm install
npm run dev
# http://localhost:5173
```

## What OpsPulse Does

- **Application monitoring** — register HTTP services; a dedicated Worker runs
  SSRF-protected checks (DNS-rebinding safe) on configurable intervals, tracks
  status (Up/Degraded/Down), uptime and response times with full history.
- **Real-time dashboard** — SignalR with a Redis backplane streams status
  changes, metrics and incidents to every connected client.
- **Server monitoring** — a lightweight cross-platform agent (Windows/Linux)
  reports CPU, RAM, disk and network via hashed API keys; stale agents are
  marked offline automatically.
- **Database monitoring** — PostgreSQL and SQL Server availability, active/max
  connections and size; credentials encrypted with AES-GCM at rest.
- **Kubernetes & Docker** — cluster nodes/pods with restart and failure
  tracking; container inventory with live CPU/memory from Docker Engine.
- **Alerts & incidents** — threshold rules with sustained windows and
  cooldowns raise deduplicated incidents automatically and resolve them on
  recovery; in-app, email (SMTP) and webhook notification channels.
- **Reports** — uptime, incident and utilization analytics with CSV export.
- **Security & audit** — JWT with refresh-token rotation (reuse detection),
  policy-based RBAC, organization-level tenant isolation, rate limiting,
  audit logging, Prometheus metrics, retention policies.

## Verification Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /health` | JSON health report incl. PostgreSQL and Redis check latency |
| `GET /api/v1/health` | API metadata (service, version, environment) |
| `GET /api/v1/auth/me` | Authenticated user profile (JWT) |
| `GET /api/v1/users` | Tenant-scoped user list (paged, filterable) |
| `GET /api/v1/services` | Monitored applications with live status |
| `GET /metrics` | Prometheus exposition endpoint |
| `GET /openapi/v1.json` | OpenAPI document |
| `GET /scalar` | Interactive API reference UI (development) |

## Authentication & Roles

- **Register** (`/register` page) creates an organization and its administrator.
- A **SuperAdmin** (`admin@opspulse.dev` / `Admin#12345` in development — configure
  via `Seed:SuperAdmin` in production) oversees all organizations.
- Access tokens expire after 15 minutes and are refreshed automatically by the
  frontend; refresh tokens are stored hashed, rotated on every use, and reuse of a
  rotated token revokes the whole chain.
- Roles: `SuperAdmin`, `OrganizationAdmin`, `DevOpsEngineer`, `Operator`, `Viewer` —
  enforced server-side via authorization policies; the UI never gates security.
- Authentication endpoints are rate-limited (20 req/min per client IP).

## Deployment

```bash
# Full stack in Docker (API + Worker + Frontend + PostgreSQL + Redis)
cp .env.example .env
# add JWT_KEY, ENCRYPTION_KEY, SEED_ADMIN_PASSWORD to .env for the full profile
docker compose --profile full up -d --build
# Frontend: http://localhost:8090 · API: http://localhost:8080

# Kubernetes
kubectl apply -f infrastructure/kubernetes/00-namespace-config.yaml
# create the real secret first (see template comments), then:
kubectl apply -f infrastructure/kubernetes/10-deployments.yaml
```

GitHub Actions run on every PR (backend build + tests, frontend lint + build);
releases publish versioned images to GHCR and attach Kubernetes artifact
bundles.

## Development Commands

```bash
# Backend (from backend/)
dotnet build OpsPulse.slnx
dotnet test OpsPulse.slnx

# New migration (from backend/)
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef -- migrations add <Name> \
  --project OpsPulse.Infrastructure --startup-project OpsPulse.API \
  --output-dir Persistence/Migrations

# Frontend (from frontend/opspulse-web/)
npm run dev      # dev server with API proxy
npm run build    # typecheck + production build
```

## Security Notes

- `.env` and `appsettings.*.Development.json` contain **local development credentials
  only** and are documented in `.env.example`; production configuration must be
  injected via environment variables or a secret store. Never commit real secrets.
- The API validates CORS origins from configuration and returns RFC 9110 problem
  details for unhandled errors without leaking internals.

## License

[MIT](LICENSE)
