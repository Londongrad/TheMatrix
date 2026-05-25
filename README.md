# TheMatrix

## English

### Overview

TheMatrix is a simulation platform for running independent simulation scenarios.

Scenarios are designed as separate simulation domains rather than as parts of one shared world.

### Scenarios

- `Classic City` is the only currently available and actively developed scenario. It is a city simulation with time progression, population behavior, economy flows, resource supply, infrastructure systems, identity, and an operator-facing web UI.
- `Metro` is a planned post-apocalyptic scenario focused on life inside an underground metro network: isolated stations, scarce resources, route control, local economies, security pressure, and survival logistics.

### What The Project Contains

- Modular .NET backend with separate bounded contexts for identity, simulation core, population, economy, resources, and city systems.
- Service projects split by API, Application, Domain, Infrastructure, and Contracts layers where that separation is useful.
- Domain models for cities, households, residents, illness, weather, resources, businesses, payroll, cashflow, and infrastructure pressure.
- Application use cases built with MediatR and FluentValidation.
- Event-driven communication through MassTransit and RabbitMQ.
- Transactional outbox for reliable integration event publishing.
- PostgreSQL persistence through Entity Framework Core and Npgsql.
- Redis-backed gateway/session/cache features.
- JWT-based external and internal authentication.
- React + TypeScript frontend built with Vite.
- Backend and frontend checks in GitHub Actions CI.

### Architecture and Service Highlights

The backend is split into services and shared building blocks:

- `Identity` is a full identity and access-management service. It includes registration/login, refresh tokens, session management, password reset/account recovery, email confirmation and email change flows, user self-service profile operations, avatar processing, admin user/role/permission management, default access policy management, security activity, cleanup jobs, and security-state propagation.
- `SimulationCore` owns the simulation clock and city lifecycle. It handles fixed-step advancement, city creation/provisioning, bootstrap state, topology, districts, anchors, residential buildings, road graphs, route resolution, weather, and active city trips.
- `Population` contains the heaviest simulation domain logic. It models residents, households, housing, needs, illness, education, employment, births, marriages, divorces, independence, weather exposure, commute pressure, household pressure, cashflow settlement, and city population progression.
- `Economy` models city money flows. It covers city budgets, allocations, budget ledger feeds, businesses, household accounts, purchases, payroll, retail sales, taxes, household obligations, billing cycles, operating cycles, and economy settlement events.
- `Resources` models city stockpiles and supply pressure. It includes stockpile seeding, resupply dispatching, stockpile advancement, emergency rationing, operational budget pressure sync, and city systems demand sync.
- `SimulationSystems` models operational infrastructure. It covers environmental conditions, power distribution, heating, sanitation, road access, snow removal, water distribution, drainage, emergency modes, maintenance dispatching, and utility incident response.
- `ApiGateway` is the external entry point/BFF. It aggregates downstream service APIs, handles identity-facing endpoints, normalizes public URLs, caches permission versions, issues internal user-context tokens, and provides gateway-level resilience around downstream calls.
- `BuildingBlocks` contains shared authentication, logging, messaging, transactional outbox, MassTransit/RabbitMQ helpers, infrastructure failure detection, and common application/domain primitives.

Services communicate through contracts and integration events. They do not share database tables directly.

### Technology Stack

#### Backend

- .NET 9 / ASP.NET Core
- Entity Framework Core 9
- PostgreSQL / Npgsql
- MassTransit / RabbitMQ
- Redis
- MediatR
- FluentValidation
- Serilog
- JWT bearer authentication
- ASP.NET Core Identity
- ImageSharp
- xUnit

#### Frontend

- React 19
- TypeScript
- Vite
- React Router
- ESLint
- Vitest
- Testing Library

#### Infrastructure and Tooling

- Docker Compose for PostgreSQL, Redis, and RabbitMQ
- GitHub Actions CI
- Centralized NuGet package management through `Directory.Packages.props`
- Solution filters for focused backend workflows
- Database migration runner project

### Repository Shape

- `TheMatrix.sln` contains the backend services, shared building blocks, tests, and tools.
- `src/services/*` contains the main backend bounded contexts.
- `src/gateways/Matrix.ApiGateway` contains the external API gateway/BFF.
- `src/building-blocks/*` contains shared backend primitives and infrastructure.
- `src/shared/Matrix.PermissionCatalog*` contains permission catalog definitions.
- `src/tools/Matrix.DatabaseMigrationRunner` contains migration execution tooling.
- `tests/*` contains backend tests grouped by service.
- `frontend/matrix-web` contains the Vite + React frontend.
- `docker-compose.yml` defines local PostgreSQL, Redis, and RabbitMQ dependencies.
- `docs/` contains development, deployment, and security notes.

### Status

TheMatrix is under active development. The repository is focused on distributed application structure, domain-heavy backend logic, asynchronous integration, security boundaries, persistence, frontend delivery, tests, and CI.

The next architectural step is to extract scenario-agnostic building blocks from Classic City before expanding the Metro scenario. Shared concepts such as districts, route access, anchors, population primitives, resource flows, and economy primitives should move into reusable modules so that new scenarios do not duplicate Classic City logic.

## Русский

### Обзор

TheMatrix — это платформа симуляций для запуска независимых симуляционных сценариев.

Сценарии проектируются как отдельные симуляционные домены, а не как части одного общего мира.

### Сценарии

- `Classic City` — единственный сейчас доступный и активно разрабатываемый сценарий. Это симуляция города с течением времени, поведением населения, экономикой, ресурсами, инфраструктурными системами, identity и веб-интерфейсом оператора.
- `Metro` — запланированный постапокалиптический сценарий про жизнь внутри подземной сети метро: изолированные станции, дефицит ресурсов, контроль маршрутов, локальные экономики, давление безопасности и логистика выживания.

### Что есть в проекте

- Модульный .NET backend с отдельными bounded contexts для identity, simulation core, population, economy, resources и city systems.
- Проекты сервисов разделены на API, Application, Domain, Infrastructure и Contracts слои.
- Доменные модели для cities, households, residents, illness, weather, resources, businesses, payroll, cashflow и infrastructure pressure.
- Application use cases на MediatR и FluentValidation.
- Event-driven коммуникация через MassTransit и RabbitMQ.
- Transactional outbox для надежной публикации integration events.
- PostgreSQL persistence через Entity Framework Core.
- Redis-backed gateway/session/cache возможности.
- JWT-based внешняя и внутренняя authentication.
- React + TypeScript frontend на Vite.
- Backend и frontend проверки в GitHub Actions CI.

### Архитектура и сильные стороны сервисов

Backend разделен на сервисы и shared building blocks:

- `Identity` — полноценный identity и access-management service. В нем есть registration/login, refresh tokens, session management, password reset/account recovery, email confirmation и email change flows, self-service profile operations, avatar processing, admin управление users/roles/permissions, default access policy, security activity, cleanup jobs и распространение security state.
- `SimulationCore` владеет simulation clock и жизненным циклом города. Он отвечает за fixed-step advancement, city creation/provisioning, bootstrap state, topology, districts, anchors, residential buildings, road graphs, route resolution, weather и active city trips.
- `Population` содержит самую насыщенную simulation domain logic. Он моделирует residents, households, housing, needs, illness, education, employment, births, marriages, divorces, independence, weather exposure, commute pressure, household pressure, cashflow settlement и city population progression.
- `Economy` моделирует денежные потоки города. В нем есть city budgets, allocations, budget ledger feeds, businesses, household accounts, purchases, payroll, retail sales, taxes, household obligations, billing cycles, operating cycles и economy settlement events.
- `Resources` моделирует городские stockpiles и supply pressure. Он включает stockpile seeding, resupply dispatching, stockpile advancement, emergency rationing, operational budget pressure sync и city systems demand sync.
- `SimulationSystems` моделирует operational infrastructure. Он покрывает environmental conditions, power distribution, heating, sanitation, road access, snow removal, water distribution, drainage, emergency modes, maintenance dispatching и utility incident response.
- `ApiGateway` — внешняя точка входа/BFF. Он агрегирует downstream service APIs, обслуживает identity-facing endpoints, normalizes public URLs, caches permission versions, выпускает internal user-context tokens и добавляет gateway-level resilience вокруг downstream calls.
- `BuildingBlocks` содержит общие authentication, logging, messaging, transactional outbox, MassTransit/RabbitMQ helpers, infrastructure failure detection и application/domain primitives.

Сервисы общаются через contracts и integration events. Прямого разделения одних и тех же таблиц между сервисами нет.

### Технологический стек

#### Backend

- .NET 9 / ASP.NET Core
- Entity Framework Core 9
- PostgreSQL / Npgsql
- MassTransit / RabbitMQ
- Redis
- MediatR
- FluentValidation
- Serilog
- JWT bearer authentication
- ASP.NET Core Identity
- ImageSharp
- xUnit

#### Frontend

- React 19
- TypeScript
- Vite
- React Router
- ESLint
- Vitest
- Testing Library

#### Инфраструктура и инструменты

- Docker Compose для PostgreSQL, Redis и RabbitMQ
- GitHub Actions CI
- Centralized NuGet package management через `Directory.Packages.props`
- Solution filters для focused backend workflows
- Database migration runner project

### Структура репозитория

- `TheMatrix.sln` содержит backend services, shared building blocks, tests и tools.
- `src/services/*` содержит основные backend bounded contexts.
- `src/gateways/Matrix.ApiGateway` содержит external API gateway/BFF.
- `src/building-blocks/*` содержит shared backend primitives и infrastructure.
- `src/shared/Matrix.PermissionCatalog*` содержит permission catalog definitions.
- `src/tools/Matrix.DatabaseMigrationRunner` содержит migration execution tooling.
- `tests/*` содержит backend tests, сгруппированные по сервисам.
- `frontend/matrix-web` содержит Vite + React frontend.
- `docker-compose.yml` описывает локальные PostgreSQL, Redis и RabbitMQ dependencies.
- `docs/` содержит development, deployment и security notes.

### Статус

TheMatrix находится в активной разработке. Репозиторий сфокусирован на структуре distributed application, сложной доменной логике backend, asynchronous integration, security boundaries, persistence, frontend delivery, tests и CI.

Следующий архитектурный шаг — вынести scenario-agnostic building blocks из Classic City перед развитием сценария Metro. Общие концепции вроде районов, route access, anchors, базовых primitives населения, потоков ресурсов и экономических primitives должны перейти в reusable modules, чтобы новые сценарии не дублировали логику Classic City.
