# Database Migrations

## Startup policy

Regular API services now follow `DatabaseStartup` policy:

- `Development`: automatic migrations and startup seed are enabled by default.
- non-`Development`: automatic migrations and startup seed are disabled by default.

Use these flags only for explicit one-off startup overrides:

- `DatabaseStartup__ApplyMigrationsOnStartup=true`
- `DatabaseStartup__RunSeedOnStartup=true`

For staging and production, prefer the dedicated migration runner instead of enabling startup migrations on the API pods.

## Migration runner

Use [C:\Users\Londongrad\RiderProjects\TheMatrix\src\tools\Matrix.DatabaseMigrationRunner\Program.cs](C:\Users\Londongrad\RiderProjects\TheMatrix\src\tools\Matrix.DatabaseMigrationRunner\Program.cs) as the explicit migration job entrypoint.

### Run one service

```powershell
$env:ConnectionStrings__SimulationCoreDb = "Host=db;Port=5432;Database=simulationcore;Username=matrix_migrator;Password=..."
dotnet run --project C:\Users\Londongrad\RiderProjects\TheMatrix\src\tools\Matrix.DatabaseMigrationRunner\Matrix.DatabaseMigrationRunner.csproj -- --service simulationcore
```

### Run all services

```powershell
$env:ConnectionStrings__IdentityDb = "..."
$env:ConnectionStrings__EconomyDb = "..."
$env:ConnectionStrings__PopulationDb = "..."
$env:ConnectionStrings__ResourcesDb = "..."
$env:ConnectionStrings__SimulationCoreDb = "..."
$env:ConnectionStrings__SimulationSystemsDb = "..."

dotnet run --project C:\Users\Londongrad\RiderProjects\TheMatrix\src\tools\Matrix.DatabaseMigrationRunner\Matrix.DatabaseMigrationRunner.csproj -- --service all
```

### Explicit one-off connection string

```powershell
dotnet run --project C:\Users\Londongrad\RiderProjects\TheMatrix\src\tools\Matrix.DatabaseMigrationRunner\Matrix.DatabaseMigrationRunner.csproj -- --service identity --connection "Host=db;Port=5432;Database=identity;Username=matrix_migrator;Password=..."
```

## CI/CD and init-container usage

Recommended deployment order:

1. Build or publish the migration runner artifact.
2. Run the migration runner as a CI/CD step or init-container with migration credentials.
3. Deploy the regular API services with runtime application credentials.

The runner logs:

- whether there are pending migrations;
- which migrations are about to be applied;
- which migrations were applied.

## Credentials split

Use separate principals:

- `migration user`: schema change permissions, used only by the migration runner;
- `app user`: normal runtime permissions, used by the API services.

Do not give the regular API runtime principal broad schema-alter privileges in production.

## Identity seed

Identity startup seed is still available behind `DatabaseStartup__RunSeedOnStartup=true`, but it should be treated as an explicit bootstrap operation, not as normal production startup behavior.
