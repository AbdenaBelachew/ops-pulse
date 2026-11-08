# Milestone Checklist and Progress Log

## Milestones

| # | Milestone | Status |
|---|-----------|--------|
| 1 | Foundation: solution, PostgreSQL, Redis, Docker, React, Tailwind | ✅ Complete (2026-11-08) |
| 2 | Identity: authentication, JWT, roles, organizations | ⬜ Not started |
| 3 | Application registration and HTTP monitoring engine | ⬜ Not started |
| 4 | Background scheduler and monitoring history | ⬜ Not started |
| 5 | SignalR and real-time dashboard | ⬜ Not started |
| 6 | Server monitoring agent | ⬜ Not started |
| 7 | Database monitoring | ⬜ Not started |
| 8 | Alert rules and incidents | ⬜ Not started |
| 9 | Notifications and reporting | ⬜ Not started |
| 10 | Kubernetes integration | ⬜ Not started |
| 11 | Docker monitoring | ⬜ Not started |
| 12 | Testing, performance, security, documentation, CI/CD, deployment | ⬜ Not started |

## Milestone 1 — Foundation (2026-11-08)

### Delivered

- .NET 10 solution (`backend/OpsPulse.slnx`) with 7 projects and clean-architecture
  dependency direction: `Domain ← Application ← Infrastructure ← API / Worker`;
  `Agent` and `Tests` standalone.
- Compatible NuGet packages (latest stable at time of install):
  `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`, `EFCore.NamingConventions 10.0.1`,
  `Microsoft.EntityFrameworkCore.Design 10.0.14`, `Serilog.AspNetCore 10.0.0`,
  `Scalar.AspNetCore 2.19.0`, `AspNetCore.HealthChecks.NpgSql 9.0.0`,
  `AspNetCore.HealthChecks.Redis 9.0.0`,
  `Microsoft.Extensions.Caching.StackExchangeRedis 10.0.14`,
  `Microsoft.OpenApi 2.13.0`, `dotnet-ef 10.0.14` (local tool manifest).
- Docker Compose: `postgres:17-alpine` + `redis:8-alpine` with healthchecks and named
  volumes; credentials via `.env` (`.env.example` committed, `.env` gitignored).
- Initial Domain entities: `BaseEntity`, `Organization`, `MonitoredService`
  (+ `ServiceEnvironment` enum) with relational configuration — unique and composite
  indexes, check constraints, cascade FK, `timestamptz` audit columns.
- `OpsPulseDbContext` with snake_case naming convention, retry-on-failure and
  automatic `UpdatedAtUtc` tracking.
- `InitialCreate` migration applied to PostgreSQL.
- API host: Serilog (config-driven), global exception handler with problem details,
  CORS (config-driven origins), health checks (`/health` returns JSON with per-check
  latency), `GET /api/v1/health` metadata endpoint, OpenAPI + Scalar UI.
- Worker and Agent: hosted-service stubs with structured logging.
- Tests: 3 domain unit tests passing.
- Frontend: Vite 8 + React 19 + TypeScript 7 + Tailwind CSS 4 (v4 plugin, theme
  tokens for the dark-navy design system), React Router 7, TanStack Query 5,
  axios, Zustand. Overview page renders live status of API, PostgreSQL and Redis
  through the dev-server proxy (`/api`, `/health`, `/hubs` → `http://localhost:5000`).

### Verification results

- `dotnet build OpsPulse.slnx` — 0 warnings, 0 errors
- `dotnet test` — 3/3 passed
- `docker compose up -d` — postgres + redis healthy
- `dotnet ef database update` — applied; schema verified (`organizations`,
  `monitored_services` with indexes/constraints)
- `GET /health` → `{"status":"Healthy","entries":{"postgresql":"Healthy","redis":"Healthy"}}`
- `GET /api/v1/health` → service metadata JSON
- `GET /openapi/v1.json` → 200; Scalar UI at `/scalar`
- `npm run build` (frontend) — clean; `tsc -b` clean
- Vite dev server on 5173 proxies `/api/v1/health` and `/health` to the API successfully

### Decisions and notes

1. **`.slnx` solution format** — the .NET 10 SDK creates the XML-based solution format
   by default. All .NET 10 tooling supports it.
2. **Dev ports 5544 (PostgreSQL) and 6399 (Redis)** — the development machine has a
   locally installed PostgreSQL on 5432 and a Redis on 6379, which silently captured
   traffic meant for the containers. OpsPulse binds non-default host ports to avoid
   the clash; container-side ports remain standard.
3. **`Microsoft.OpenApi` pinned to 2.13.0** — the template's 2.0.0 has a known high
   severity advisory (NU1903) and 3.x breaks the `Microsoft.AspNetCore.OpenApi`
   source generator; 2.13.0 is the patched 2.x line.
4. **OpenAPI via built-in `AddOpenApi` + Scalar UI** — first-party document generation
   for .NET 10; replaces Swashbuckle.
5. **EF Core check constraints** — in EF 10, `TableBuilder.HasCheckConstraint` returns
   `CheckConstraintBuilder` and cannot be fluently chained; use a statement lambda.
6. **Dev credentials** (`opspulse` / `opspulse_dev`) are local-only and documented;
   production must inject connection strings via environment variables or secret stores.
