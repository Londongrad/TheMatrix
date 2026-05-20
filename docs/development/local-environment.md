# Local Development Environment

## What this repo expects

For a normal local setup you need:

- .NET SDK pinned by `global.json`
- Node.js 22+
- Docker Desktop or another local Docker runtime
- PowerShell

The local stack is split into two parts:

- infrastructure in `docker-compose.yml`
- application processes started from the solution and the frontend workspace

## 1. Create the local compose env file

Copy the example file and replace the placeholder passwords:

```powershell
Copy-Item .env.example .env
```

The compose file requires values for:

- Postgres
- Redis
- RabbitMQ

`.env` is ignored by git, so it is safe to keep machine-local values there.

## 2. Start the local infrastructure

```powershell
docker compose up -d postgres redis rabbitmq
```

Useful follow-ups:

```powershell
docker compose ps
docker compose logs -f postgres
docker compose logs -f redis
docker compose logs -f rabbitmq
```

To reset the local infra volumes:

```powershell
docker compose down -v
```

## 3. Configure local application secrets

Use the user-secrets guide for the entrypoint projects that already support it:

- `docs/development/user-secrets.md`

Today these entrypoints are wired to ASP.NET Core user-secrets:

- API Gateway
- Identity API
- SimulationCore API
- Population API

The remaining APIs still rely on local environment variables or ignored development config on your machine for their secret values.

## 4. Apply database migrations

In `Development`, APIs can still apply migrations automatically by startup policy.

If you want an explicit migration step instead, use the dedicated migration runner:

- `docs/deployment/database-migrations.md`

## 5. Start the backend services

The normal local backend shape is:

- Identity API
- SimulationCore API
- Population API
- Economy API
- Resources API
- SimulationSystems API
- API Gateway

You can run them from Rider/Visual Studio launch profiles or with `dotnet run --project ...`.

Tracked local service URLs currently expect this downstream shape:

- Identity: `https://localhost:7256`
- SimulationCore: `https://localhost:7207`
- Population: `https://localhost:7297`
- Economy: `http://localhost:5286`
- Resources: `https://localhost:7319`
- SimulationSystems: `https://localhost:7318`

## SimulationCore fixed-step tick engine

SimulationCore runs its background clock with two separate time concepts:

- `SimulationCore:Tick:PeriodMilliseconds` is the scheduler wake-up cadence. It controls how often the hosted service wakes up locally. It is not the simulation delta.
- `SimulationCore:Tick:FixedStepSeconds` is the simulation step size consumed by the clock when enough pending simulation time exists.
- `SimulationCore:Tick:MaxStepsPerSimulationPerCycle` caps how many fixed steps one simulation can process during one scheduler cycle.

Default local values:

- `PeriodMilliseconds = 1000`
- `FixedStepSeconds = 60`
- `MaxStepsPerSimulationPerCycle = 10`

On each scheduler cycle, SimulationCore measures the real elapsed time since the previous cycle. For each running simulation, that real delta is scaled by the simulation speed and accumulated into persisted pending simulation time. Leftover pending time remains stored on the simulation clock and carries over to later cycles.

Example: with speed `60x`, scheduler `realDelta = 1s`, and `FixedStepSeconds = 60`, exactly `60s` of simulation time becomes due, so one fixed simulation step is processed.

Backlog example: if the scheduler stalls for `30` real seconds at speed `60x`, then `30` simulation minutes become due. With `FixedStepSeconds = 60` and `MaxStepsPerSimulationPerCycle = 10`, only `10` one-minute steps are processed in that cycle. The remaining `20` minutes stay pending and are processed by later cycles under the same cap.

## 6. Start the frontend

```powershell
Set-Location frontend/matrix-web
npm ci
npm run dev
```

`frontend/matrix-web/.env.development` should continue to point the frontend at the local HTTPS gateway:

- `VITE_API_BASE_URL=https://localhost:7155`

The frontend supports two local modes:

- Recommended full local mode: `https://localhost:5173`
- requires local HTTPS certificates in `frontend/matrix-web/certs`
- supports the gateway secure refresh-cookie flow against `https://localhost:7155`

- Fallback clean-checkout mode: `http://localhost:5173`
- starts automatically when the certificate files are missing
- useful for `npm run build` and basic frontend startup on a clean checkout
- full auth refresh/logout cookie flow is expected to use HTTPS because the gateway refresh cookie is `Secure` and `SameSite=Strict`

Expected local certificate files:

- `frontend/matrix-web/certs/localhost-key.pem`
- `frontend/matrix-web/certs/localhost.pem`

If you want the recommended HTTPS mode locally, generate the certificates on your machine and keep them untracked. A typical `mkcert` flow is:

```powershell
Set-Location frontend/matrix-web
New-Item -ItemType Directory -Force certs | Out-Null
mkcert -key-file certs/localhost-key.pem -cert-file certs/localhost.pem localhost 127.0.0.1 ::1
```

Do not commit generated certificate files.

## Frontend quality check

Run the frontend quality gate locally with:

```powershell
Set-Location frontend/matrix-web
npm run check
```

`npm run check` runs:

- `npm run lint`
- `npm run build`

The build is expected to pass even without local Vite certificates because the frontend falls back to `http://localhost:5173` when `frontend/matrix-web/certs` is absent.

## 7. Quick sanity check

After the stack is up:

- the frontend should load on `https://localhost:5173` in the recommended full local mode, or on `http://localhost:5173` when local Vite certificates are absent
- the gateway should be able to reach downstream services
- RabbitMQ management should be available on `http://localhost:15672`
- Postgres and Redis containers should report healthy/running in `docker compose ps`

## Related docs

- `docs/development/user-secrets.md`
- `docs/deployment/database-migrations.md`
- `docs/security/trusted-client-ip-forwarding.md`
- `docs/security/internal-jwt-key-rotation.md`
