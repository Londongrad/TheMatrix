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

## 6. Start the frontend

```powershell
Set-Location frontend/matrix-web
npm ci
npm run dev
```

The frontend dev server runs on:

- `http://localhost:5173`

## 7. Quick sanity check

After the stack is up:

- the frontend should load on `http://localhost:5173`
- the gateway should be able to reach downstream services
- RabbitMQ management should be available on `http://localhost:15672`
- Postgres and Redis containers should report healthy/running in `docker compose ps`

## Related docs

- `docs/development/user-secrets.md`
- `docs/deployment/database-migrations.md`
- `docs/security/trusted-client-ip-forwarding.md`
- `docs/security/internal-jwt-key-rotation.md`
