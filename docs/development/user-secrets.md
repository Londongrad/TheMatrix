# Local User Secrets

Development secrets are no longer tracked in `appsettings.Development.json`.

ASP.NET Core user-secrets are currently enabled for these entrypoint projects:

- `src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj`
- `src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj`
- `src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj`
- `src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj`

## Common secret categories

Most local runs need some combination of:

- database connection strings
- RabbitMQ credentials
- Redis connection string
- external JWT signing key
- internal identity API key ring
- internal user-context JWT key ring
- internal service JWT key ring

## Typical setup

### Identity API

```powershell
dotnet user-secrets set "ConnectionStrings:IdentityDb" "<identity-db-connection-string>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "ExternalJwt:SigningKey" "<external-jwt-signing-key>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "IdentityInternal:CurrentKeyId" "primary" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "IdentityInternal:Keys:primary" "<identity-internal-api-key>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
```

### API Gateway

```powershell
dotnet user-secrets set "ExternalJwt:SigningKey" "<external-jwt-signing-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "IdentityInternal:BaseUrl" "https://localhost:7256" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "IdentityInternal:CurrentKeyId" "primary" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "IdentityInternal:Keys:primary" "<identity-internal-api-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "Redis:ConnectionString" "<redis-connection-string>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalUserContextJwt:Issuer" "<internal-user-jwt-issuer>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalUserContextJwt:Audience" "<internal-user-jwt-audience>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalUserContextJwt:CurrentKeyId" "primary" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalUserContextJwt:Keys:primary" "<internal-user-jwt-signing-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalUserContextJwt:LifetimeSeconds" "60" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
```

### SimulationCore API

```powershell
dotnet user-secrets set "ConnectionStrings:SimulationCoreDb" "<simulationcore-db-connection-string>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Issuer" "<internal-user-jwt-issuer>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Audience" "<internal-user-jwt-audience>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:CurrentKeyId" "primary" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Keys:primary" "<internal-user-jwt-signing-key>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:LifetimeSeconds" "60" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Issuer" "<internal-service-jwt-issuer>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Audience" "<internal-service-jwt-audience>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalServiceJwt:CurrentKeyId" "primary" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Keys:primary" "<internal-service-jwt-signing-key>" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
dotnet user-secrets set "InternalServiceJwt:LifetimeSeconds" "60" --project src/services/simulationcore/Matrix.SimulationCore.Api/Matrix.SimulationCore.Api.csproj
```

### Population API

```powershell
dotnet user-secrets set "ConnectionStrings:PopulationDb" "<population-db-connection-string>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Issuer" "<internal-user-jwt-issuer>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Audience" "<internal-user-jwt-audience>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:CurrentKeyId" "primary" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:Keys:primary" "<internal-user-jwt-signing-key>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalUserContextJwt:LifetimeSeconds" "60" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Issuer" "<internal-service-jwt-issuer>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Audience" "<internal-service-jwt-audience>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalServiceJwt:CurrentKeyId" "primary" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalServiceJwt:Keys:primary" "<internal-service-jwt-signing-key>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalServiceJwt:LifetimeSeconds" "60" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
```

## Useful commands

```powershell
dotnet user-secrets list --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets remove "ExternalJwt:SigningKey" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets clear --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
```
