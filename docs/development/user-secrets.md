# Local User Secrets

Development secrets are no longer stored in `appsettings.Development.json`.

ASP.NET Core now loads local secrets automatically for these entrypoint projects:

- `src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj`
- `src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj`
- `src/services/citycore/Matrix.CityCore.Api/Matrix.CityCore.Api.csproj`
- `src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj`

## Typical setup

### Identity API

```powershell
dotnet user-secrets set "ConnectionStrings:IdentityDb" "<identity-db-connection-string>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "ExternalJwt:SigningKey" "<external-jwt-signing-key>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "IdentityInternal:ApiKey" "<identity-internal-api-key>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
```

### API Gateway

```powershell
dotnet user-secrets set "ExternalJwt:SigningKey" "<external-jwt-signing-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "IdentityInternal:ApiKey" "<identity-internal-api-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "Redis:ConnectionString" "<redis-connection-string>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
dotnet user-secrets set "InternalJwt:SigningKey" "<internal-jwt-signing-key>" --project src/gateways/Matrix.ApiGateway/Matrix.ApiGateway.csproj
```

### CityCore API

```powershell
dotnet user-secrets set "ConnectionStrings:CityCoreDb" "<citycore-db-connection-string>" --project src/services/citycore/Matrix.CityCore.Api/Matrix.CityCore.Api.csproj
dotnet user-secrets set "InternalJwt:SigningKey" "<internal-jwt-signing-key>" --project src/services/citycore/Matrix.CityCore.Api/Matrix.CityCore.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/citycore/Matrix.CityCore.Api/Matrix.CityCore.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/citycore/Matrix.CityCore.Api/Matrix.CityCore.Api.csproj
```

### Population API

```powershell
dotnet user-secrets set "ConnectionStrings:PopulationDb" "<population-db-connection-string>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "InternalJwt:SigningKey" "<internal-jwt-signing-key>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "RabbitMq:Username" "<rabbitmq-username>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
dotnet user-secrets set "RabbitMq:Password" "<rabbitmq-password>" --project src/services/population/Matrix.Population.Api/Matrix.Population.Api.csproj
```

## Useful commands

```powershell
dotnet user-secrets list --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets remove "ExternalJwt:SigningKey" --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
dotnet user-secrets clear --project src/services/identity/Matrix.Identity.Api/Matrix.Identity.Api.csproj
```
