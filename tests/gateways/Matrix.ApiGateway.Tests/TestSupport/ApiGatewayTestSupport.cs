using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MassTransit;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Economy;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Controllers.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Education;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Requests;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;
using Matrix.Education.Contracts.Enrollments;
using Matrix.Education.Contracts.Institutions;
using Matrix.Education.Contracts.Students;
using Matrix.Identity.Contracts.Internal.Responses;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Weather.Views;
using Matrix.SimulationCore.Contracts.Simulation.Requests;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Common.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Tests.TestSupport
{
    public static class ApiGatewayTestSupport
    {
        private const string CurrentSigningKey = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&";
        private const string NextSigningKey = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$";

        public static IOptions<AuthContextOptions> CreateAuthContextOptions(int cacheTtlSeconds = 1800)
        {
            return Options.Create(
                new AuthContextOptions
                {
                    CacheTtlSeconds = cacheTtlSeconds
                });
        }

        public static IOptions<PermissionsVersionOptions> CreatePermissionsVersionOptions(
            int cacheTtlSeconds = 300,
            int staleCacheTtlSeconds = 21600,
            bool allowStaleCacheOnIdentityFailure = true)
        {
            return Options.Create(
                new PermissionsVersionOptions
                {
                    CacheTtlSeconds = cacheTtlSeconds,
                    StaleCacheTtlSeconds = staleCacheTtlSeconds,
                    AllowStaleCacheOnIdentityFailure = allowStaleCacheOnIdentityFailure
                });
        }

        public static IOptions<InternalUserContextJwtOptions> CreateInternalJwtOptions(int lifetimeSeconds = 60)
        {
            return Options.Create(
                new InternalUserContextJwtOptions
                {
                    Issuer = "matrix-gateway",
                    Audience = "matrix-internal",
                    LifetimeSeconds = lifetimeSeconds,
                    CurrentKeyId = "kid-current",
                    SigningKey = CurrentSigningKey,
                    Keys = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["kid-current"] = CurrentSigningKey,
                        ["kid-next"] = NextSigningKey
                    }
                });
        }

        public static IOptions<ClassicCitySetupSessionOptions> CreateClassicCitySetupSessionOptions(
            int recentDraftReuseWindowSeconds = 30,
            int launchQueueRecoveryDelaySeconds = 20,
            bool reconciliationEnabled = true,
            int reconciliationIntervalSeconds = 15)
        {
            return Options.Create(
                new ClassicCitySetupSessionOptions
                {
                    RecentDraftReuseWindowSeconds = recentDraftReuseWindowSeconds,
                    LaunchQueueRecoveryDelaySeconds = launchQueueRecoveryDelaySeconds,
                    ReconciliationEnabled = reconciliationEnabled,
                    ReconciliationIntervalSeconds = reconciliationIntervalSeconds
                });
        }

        public static TimeProvider CreateTimeProvider(DateTimeOffset utcNow)
        {
            return new FrozenTimeProvider(utcNow);
        }

        public static IServiceProvider CreateServiceProvider(IPermissionsVersionStore? permissionsVersionStore = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            if (permissionsVersionStore is not null)
                services.AddSingleton(permissionsVersionStore);

            return services.BuildServiceProvider();
        }

        public static IHttpContextAccessor CreateHttpContextAccessor(
            Guid userId,
            string? jti = null)
        {
            DefaultHttpContext httpContext = new();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtRegisteredClaimNames.Sub,
                            value: userId.ToString()),
                        ..CreateOptionalClaims(jti)
                    ],
                    authenticationType: "gateway"));

            return new HttpContextAccessor
            {
                HttpContext = httpContext
            };
        }

        public static ClassicCitySetupDraftDto CreateClassicCitySetupDraft(
            string name = "Novy Mir",
            string generationSeed = "seed-001",
            DateTimeOffset? startSimTimeUtc = null,
            string currentWeatherMode = "Random")
        {
            DateTimeOffset effectiveStart = startSimTimeUtc ??
            new DateTimeOffset(
                year: 2048,
                month: 6,
                day: 1,
                hour: 8,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return new ClassicCitySetupDraftDto(
                Name: name,
                StartSimTimeLocal: "2048-06-01T17:00",
                StartSimTimeUtc: effectiveStart,
                SpeedMultiplier: "1.5",
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: "540",
                GenerationSeed: generationSeed,
                InitialWeatherMode: currentWeatherMode,
                InitialWeatherType: "Clear",
                InitialWeatherSeverity: "Mild",
                InitialWeatherTemperatureC: "",
                PopulationTargetMode: "Preset10K",
                SizeTier: "Medium",
                UrbanDensity: "Balanced",
                DevelopmentLevel: "Balanced",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Balanced",
                PlannedPeopleCount: "");
        }

        public static ClassicCitySetupSessionState CreateClassicCitySetupSessionState(
            Guid sessionId,
            Guid ownerUserId,
            string status = "Draft",
            string currentStepId = "scenario",
            ClassicCitySetupDraftDto? draft = null,
            DateTimeOffset? updatedAtUtc = null)
        {
            DateTimeOffset timestamp = updatedAtUtc ??
            new DateTimeOffset(
                year: 2048,
                month: 6,
                day: 1,
                hour: 9,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return new ClassicCitySetupSessionState
            {
                SessionId = sessionId,
                OwnerUserId = ownerUserId,
                ScenarioKind = "ClassicCity",
                Status = status,
                CurrentStepId = currentStepId,
                Draft = draft ?? CreateClassicCitySetupDraft(),
                CreatedAtUtc = timestamp.AddMinutes(-5),
                UpdatedAtUtc = timestamp
            };
        }

        public static ClassicCitySetupSessionView CreateClassicCitySetupSessionView(
            Guid? sessionId = null,
            string status = "Draft",
            string currentStepId = "scenario",
            ClassicCitySetupDraftDto? draft = null,
            Guid? cityId = null,
            CityProvisioningView? provisioning = null,
            string? failureCode = null,
            string? failureMessage = null,
            DateTimeOffset? createdAtUtc = null,
            DateTimeOffset? updatedAtUtc = null,
            DateTimeOffset? launchQueuedAtUtc = null,
            DateTimeOffset? startedAtUtc = null,
            DateTimeOffset? completedAtUtc = null)
        {
            DateTimeOffset effectiveCreatedAtUtc =
                createdAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero);
            DateTimeOffset effectiveUpdatedAtUtc =
                updatedAtUtc ?? effectiveCreatedAtUtc.AddMinutes(5);

            return new ClassicCitySetupSessionView(
                SessionId: sessionId ?? Guid.Parse("c289f553-c877-4ac4-b24a-f01be13ce25e"),
                ScenarioKind: "ClassicCity",
                Status: status,
                CurrentStepId: currentStepId,
                Draft: draft ?? CreateClassicCitySetupDraft(),
                CityId: cityId,
                Provisioning: provisioning,
                FailureCode: failureCode,
                FailureMessage: failureMessage,
                CreatedAtUtc: effectiveCreatedAtUtc,
                UpdatedAtUtc: effectiveUpdatedAtUtc,
                LaunchQueuedAtUtc: launchQueuedAtUtc,
                StartedAtUtc: startedAtUtc,
                CompletedAtUtc: completedAtUtc);
        }

        public static ClassicCitySetupSessionLaunchAuthSnapshot CreateLaunchAuthSnapshot(
            Guid userId,
            string? jti = "launch-jti",
            int permissionsVersion = 7,
            DateTimeOffset? capturedAtUtc = null,
            params string[] effectivePermissions)
        {
            return new ClassicCitySetupSessionLaunchAuthSnapshot
            {
                UserId = userId,
                Jti = jti,
                PermissionsVersion = permissionsVersion,
                EffectivePermissions = effectivePermissions.Length == 0
                    ? ["city.launch"]
                    : effectivePermissions,
                CapturedAtUtc = capturedAtUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero)
            };
        }

        public static CreateCityRequestDto CreateCityLaunchRequest(
            string name = "Novy Mir",
            Guid? provisioningCorrelationId = null,
            int? plannedPeopleCount = 10000)
        {
            return new CreateCityRequestDto(
                Name: name,
                StartSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                SpeedMultiplier: 1.5m,
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 540,
                GenerationSeed: "seed-001",
                SizeTier: "Medium",
                UrbanDensity: "Balanced",
                DevelopmentLevel: "Balanced",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Balanced",
                InitialWeatherMode: "Random",
                InitialWeatherType: null,
                InitialWeatherSeverity: null,
                InitialWeatherTemperatureC: null,
                PlannedPeopleCount: plannedPeopleCount,
                ProvisioningCorrelationId: provisioningCorrelationId);
        }

        public static CityProvisioningView CreateCityProvisioningView(
            Guid cityId,
            string populationStatus = "Completed",
            string economyStatus = "Completed",
            string? populationFailureCode = null,
            string? economyFailureCode = null)
        {
            return new CityProvisioningView(
                CityId: cityId,
                PopulationBootstrap: new CityPopulationBootstrapView(
                    OperationId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Status: populationStatus,
                    PlannedPeopleCount: 10000,
                    ResidentialCapacity: 12000,
                    Summary: new CityPopulationBootstrapSummaryView(
                        CityId: cityId,
                        RequestedPeopleCount: 10000,
                        GeneratedPeopleCount: 10000,
                        HouseholdCount: 3500,
                        HousedHouseholdCount: 3400,
                        HomelessHouseholdCount: 100,
                        HousedPeopleCount: 9800,
                        HomelessPeopleCount: 200),
                    FailureCode: populationFailureCode),
                EconomyBootstrap: new CityEconomyBootstrapView(
                    OperationId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Status: economyStatus,
                    FailureCode: economyFailureCode,
                    UnitKind: "Currency",
                    UnitCode: "CR",
                    UnitDisplayName: "Credits",
                    UnitSymbol: "C"));
        }

        public static CityProvisioningStatusView CreateCityProvisioningStatusView(
            Guid cityId,
            string status = "Active",
            string? populationFailureCode = null,
            string? economyFailureCode = null,
            DateTimeOffset? populationCompletedAtUtc = null,
            DateTimeOffset? economyCompletedAtUtc = null,
            DateTimeOffset? populationFailedAtUtc = null,
            DateTimeOffset? economyFailedAtUtc = null)
        {
            var completedAtUtc = new DateTimeOffset(
                year: 2048,
                month: 6,
                day: 1,
                hour: 11,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return new CityProvisioningStatusView(
                CityId: cityId,
                Status: status,
                PopulationBootstrapOperationId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                EconomyBootstrapOperationId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                PopulationBootstrapFailureCode: populationFailureCode,
                EconomyBootstrapFailureCode: economyFailureCode,
                PopulationBootstrapCompletedAtUtc: populationCompletedAtUtc ??
                                                   (status == "Active"
                                                       ? completedAtUtc
                                                       : null),
                EconomyBootstrapCompletedAtUtc: economyCompletedAtUtc ??
                                                (status == "Active"
                                                    ? completedAtUtc
                                                    : null),
                PopulationBootstrapFailedAtUtc: populationFailedAtUtc,
                EconomyBootstrapFailedAtUtc: economyFailedAtUtc,
                ProvisioningStartedAtUtc: completedAtUtc.AddMinutes(-10),
                ProvisioningHeartbeatAtUtc: completedAtUtc.AddMinutes(-1),
                ProvisioningLeaseExpiresAtUtc: completedAtUtc.AddMinutes(4),
                ProvisioningAttemptCount: 2);
        }

        public static ClassicCitySetupSessionService CreateClassicCitySetupSessionService(
            FakeClassicCitySetupSessionStore sessionStore,
            RecordingPublishEndpoint publishEndpoint,
            IHttpContextAccessor httpContextAccessor,
            FakePermissionsVersionStore? permissionsVersionStore = null,
            FakeAuthContextStore? authContextStore = null,
            RecordingCitiesApiClient? citiesApiClient = null,
            RecordingProvisioningService? provisioningService = null,
            IInternalJwtRequestContextAccessor? internalJwtRequestContextAccessor = null,
            IOptions<ClassicCitySetupSessionOptions>? options = null,
            TimeProvider? timeProvider = null)
        {
            IInternalJwtRequestContextAccessor requestContextAccessor =
                internalJwtRequestContextAccessor ?? new InternalJwtRequestContextAccessor();

            return new ClassicCitySetupSessionService(
                sessionStore: sessionStore,
                citiesApiClient: citiesApiClient ?? new RecordingCitiesApiClient(requestContextAccessor),
                provisioningService: provisioningService ?? new RecordingProvisioningService(requestContextAccessor),
                publishEndpoint: publishEndpoint,
                httpContextAccessor: httpContextAccessor,
                permissionsVersionStore: permissionsVersionStore ?? new FakePermissionsVersionStore(),
                authContextStore: authContextStore ?? new FakeAuthContextStore(),
                internalJwtRequestContextAccessor: requestContextAccessor,
                options: options ?? CreateClassicCitySetupSessionOptions(),
                timeProvider: timeProvider ?? TimeProvider.System,
                logger: NullLogger<ClassicCitySetupSessionService>.Instance);
        }

        public static ClassicCitySetupSessionsController CreateClassicCitySetupSessionsController(
            RecordingClassicCitySetupSessionService? setupSessionService = null)
        {
            return new ClassicCitySetupSessionsController(
                setupSessionService ?? new RecordingClassicCitySetupSessionService());
        }

        public static SimulationsController CreateSimulationsController(
            RecordingSimulationApiClient? simulationClient = null)
        {
            return new SimulationsController(simulationClient ?? new RecordingSimulationApiClient());
        }

        public static CityOperationsDashboardController CreateCityOperationsDashboardController(
            RecordingCityOperationsDashboardService? dashboardService = null)
        {
            return new CityOperationsDashboardController(
                dashboardService ?? new RecordingCityOperationsDashboardService());
        }

        public static CitiesController CreateCitiesController(
            RecordingCitiesApiClient? citiesClient = null,
            RecordingTripsApiClient? tripsClient = null,
            RecordingSimulationApiClient? simulationClient = null,
            RecordingEconomyApiClient? economyClient = null,
            RecordingEducationApiClient? educationClient = null,
            RecordingPopulationApiClient? populationClient = null,
            RecordingStockpilesApiClient? stockpilesClient = null,
            RecordingEnvironmentalConditionsApiClient? environmentalConditionsClient = null,
            RecordingProvisioningService? provisioningService = null,
            TimeProvider? timeProvider = null)
        {
            IInternalJwtRequestContextAccessor requestContextAccessor = new InternalJwtRequestContextAccessor();

            return new CitiesController(
                citiesClient: citiesClient ?? new RecordingCitiesApiClient(requestContextAccessor),
                tripsClient: tripsClient ?? new RecordingTripsApiClient(),
                simulationClient: simulationClient ?? new RecordingSimulationApiClient(),
                economyClient: economyClient ?? new RecordingEconomyApiClient(),
                educationClient: educationClient ?? new RecordingEducationApiClient(),
                populationClient: populationClient ?? new RecordingPopulationApiClient(),
                stockpilesClient: stockpilesClient ?? new RecordingStockpilesApiClient(),
                environmentalConditionsClient: environmentalConditionsClient ??
                                               new RecordingEnvironmentalConditionsApiClient(),
                cityProvisioningService: provisioningService ??
                                         new RecordingProvisioningService(requestContextAccessor),
                timeProvider: timeProvider ?? TimeProvider.System,
                logger: NullLogger<CitiesController>.Instance);
        }

        public static SimulationClockView CreateSimulationClockView(
            Guid? simulationId = null,
            DateTimeOffset? simTimeUtc = null,
            decimal speed = 1.5m,
            long tickId = 42,
            string state = "Running")
        {
            Guid resolvedSimulationId = simulationId ?? Guid.Parse("54c198fd-7465-4e58-98a5-53572d474f3c");

            return new SimulationClockView(
                SimulationId: resolvedSimulationId,
                HostId: resolvedSimulationId,
                ScenarioKey: "classic-city",
                HostTypeKey: "city",
                SimTimeUtc: simTimeUtc ??
                new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 12,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                TickId: tickId,
                Speed: speed,
                State: state);
        }

        public static CityOperationsDashboardView CreateCityOperationsDashboardView(
            DateTimeOffset? generatedAtUtc = null)
        {
            DateTimeOffset timestamp = generatedAtUtc ??
            new DateTimeOffset(
                year: 2048,
                month: 6,
                day: 3,
                hour: 13,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return new CityOperationsDashboardView(
                GeneratedAtUtc: timestamp,
                TrackedHosts: CreateDashboardMetric(
                    label: "Tracked hosts",
                    current: 5),
                ReadyHosts: CreateDashboardMetric(
                    label: "Ready hosts",
                    current: 4),
                ArchivedRecords: CreateDashboardMetric(
                    label: "Archived",
                    current: 1),
                AttentionQueue: CreateDashboardMetric(
                    label: "Attention",
                    current: 2),
                EnvironmentalAlerts: CreateDashboardMetric(
                    label: "Environmental",
                    current: 1),
                PopulationDistrictAlerts: CreateDashboardMetric(
                    label: "Population",
                    current: 1),
                DistrictResponsePriorityAlerts: CreateDashboardMetric(
                    label: "District response",
                    current: 1),
                MobilityAlerts: CreateDashboardMetric(
                    label: "Mobility",
                    current: 1),
                OperationalBudgetAlerts: CreateDashboardMetric(
                    label: "Budget",
                    current: 1),
                TickFreshnessAlerts: CreateDashboardMetric(
                    label: "Tick freshness",
                    current: 0),
                PhaseProgressAlerts: CreateDashboardMetric(
                    label: "Phase progress",
                    current: 0),
                NewCities: CreateDashboardPeriodRow(label: "New cities"),
                ArchivedCities: CreateDashboardPeriodRow(label: "Archived cities"),
                FailedBootstraps: CreateDashboardPeriodRow(label: "Failed bootstraps"),
                ReadyHandOffs: CreateDashboardPeriodRow(label: "Ready handoffs"),
                Services: [],
                Events: [],
                EnvironmentalCities: [],
                PopulationDistrictCities: [],
                DistrictResponsePriorities: [],
                MobilityCities: [],
                BudgetPressureCities: [],
                TickFreshnessCities: [],
                PhaseProgressCities: [],
                AttentionCities: [],
                ReadyCities: [],
                ArchivedCitiesList: []);
        }

        public static CityPopulationDashboardDto CreateCityPopulationDashboardDto(Guid? cityId = null)
        {
            Guid resolvedCityId = cityId ?? Guid.Parse("4ca7f79d-8386-4d6c-b9fa-f4d0f6678d60");

            return new CityPopulationDashboardDto(
                CityId: resolvedCityId,
                CurrentDate: "2048-06-03",
                GeneratedAtUtc: "2048-06-03T13:00:00Z",
                Metrics:
                [
                    new CityPopulationDashboardMetricDto(
                        Key: "populationAlive",
                        Label: "Alive residents",
                        Description: "Current resident count.",
                        ValueKind: "count",
                        CurrentValue: 10240,
                        DeltaYesterday: 12,
                        DeltaMonth: 150,
                        DeltaYear: null)
                ],
                RecentEvents:
                [
                    new CityPopulationActivityEventDto(
                        ActivityEventId: Guid.Parse("b2ac2283-bf8b-4f4f-9890-0727c29f1316"),
                        CurrentDate: "2048-06-03",
                        OccurredAtUtc: "2048-06-03T12:50:00Z",
                        EventType: "Migration",
                        Source: "Population",
                        Severity: "Info",
                        Title: "New arrivals",
                        Summary: "New residents arrived.",
                        PrimaryResidentId: null,
                        SecondaryResidentId: null)
                ]);
        }

        public static EconomySummaryView CreateEconomySummaryView(
            decimal balance = 120000m,
            decimal totalGrossPayroll = 65000m,
            decimal totalIncomeTaxIncome = 15000m,
            decimal totalSalesTaxIncome = 9000m,
            decimal totalRetailTurnover = 87000m,
            decimal totalCityExpenses = 42000m)
        {
            return new EconomySummaryView(
                UnitKind: "Currency",
                UnitCode: "CR",
                UnitDisplayName: "Credits",
                UnitSymbol: "C",
                Balance: balance,
                TotalTaxIncome: totalIncomeTaxIncome + totalSalesTaxIncome,
                TotalIncomeTaxIncome: totalIncomeTaxIncome,
                TotalSalesTaxIncome: totalSalesTaxIncome,
                TotalDirectRevenue: 11000m,
                TotalCityExpenses: totalCityExpenses,
                TotalRetailTurnover: totalRetailTurnover,
                TotalGrossPayroll: totalGrossPayroll,
                TotalNetPayroll: totalGrossPayroll - totalIncomeTaxIncome);
        }

        public static PagedResult<PersonDto> CreateResidentsPageResult(Guid? personId = null)
        {
            return new PagedResult<PersonDto>(
                items:
                [
                    new PersonDto(
                        Id: personId ?? Guid.Parse("09413be1-3cb9-4738-b4b9-9f729afde852"),
                        FullName: "Mira Sol",
                        Sex: "Female",
                        BirthDate: "2024-05-21",
                        DeathDate: null,
                        Age: 24,
                        AgeGroup: "Adult",
                        LifeStatus: "Alive",
                        MaritalStatus: "Single",
                        EducationLevel: "Higher",
                        Health: 82,
                        Happiness: 71,
                        Energy: 66,
                        Stress: 23,
                        SocialNeed: 29,
                        EmploymentStatus: "Employed",
                        JobTitle: "Transit Planner")
                ],
                totalCount: 1,
                pageNumber: 2,
                pageSize: 25);
        }

        public static CityResidentDetailsDto CreateCityResidentDetailsDto(Guid? personId = null)
        {
            return new CityResidentDetailsDto(
                Id: personId ?? Guid.Parse("52f20708-a8fc-4cf2-958d-c0bcfaec20a6"),
                FullName: "Mira Sol",
                Sex: "Female",
                BirthDate: "2024-05-21",
                DeathDate: null,
                Age: 24,
                AgeGroup: "Adult",
                LifeStatus: "Alive",
                MaritalStatus: "Single",
                EducationLevel: "Higher",
                Health: 82,
                Happiness: 71,
                Energy: 66,
                Stress: 23,
                SocialNeed: 29,
                EmploymentStatus: "Employed",
                JobTitle: "Transit Planner",
                CurrentSpouse: null,
                Mother: null,
                Father: null,
                Children: [],
                LastChildbirthDate: null,
                CurrentHousing: new CityResidentHousingDto(
                    HouseholdId: Guid.Parse("07f51d4d-391d-4ca9-a549-696654e67922"),
                    HousingStatus: "Housed",
                    ResidentialBuildingId: Guid.Parse("fa999262-fad0-4557-86a4-5ca21f3efeaa")),
                CurrentWorkplace: null,
                CurrentEducationInstitution: null,
                PrimaryHealthcareProvider: null,
                CurrentActiveTrip: null);
        }

        public static CityPopulationDistrictPressureDto CreateCityPopulationDistrictPressureDto(Guid cityId)
        {
            return new CityPopulationDistrictPressureDto(
                CityId: cityId,
                GeneratedAtUtc: "2048-06-03T13:10:00Z",
                Districts:
                [
                    new CityPopulationDistrictPressureItemDto(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        ResidentCount: 1200,
                        HouseholdCount: 420,
                        HomelessResidentCount: 25,
                        AverageHealth: 78.5m,
                        AverageStress: 34.2m,
                        AverageHappiness: 62.1m,
                        ActiveIllnessCount: 18,
                        SevereIllnessCount: 3,
                        UtilityContinuityIndex: 0.76m,
                        UtilityIncidentPressureIndex: 0.34m,
                        HousingFragilityIndex: 0.22m,
                        PopulationPressureIndex: 0.41m)
                ]);
        }

        public static CityDistrictHeatingConditionsView CreateCityDistrictHeatingConditionsView(Guid cityId)
        {
            return new CityDistrictHeatingConditionsView(
                CityId: cityId,
                EffectiveTickId: 17,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero),
                HeatingSupportIndex: 0.81m,
                Districts:
                [
                    new CityDistrictHeatingConditionView(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        HeatingCoverageIndex: 0.84m,
                        HeatingSupportIndex: 0.81m,
                        OutageRiskIndex: 0.21m,
                        ComfortStressIndex: 0.18m,
                        MaintenancePriorityIndex: 0.33m)
                ]);
        }

        public static CityDistrictWaterDistributionConditionsView CreateCityDistrictWaterDistributionConditionsView(
            Guid cityId)
        {
            return new CityDistrictWaterDistributionConditionsView(
                CityId: cityId,
                EffectiveTickId: 17,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero),
                WaterSupportIndex: 0.79m,
                Districts:
                [
                    new CityDistrictWaterDistributionConditionView(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        WaterCoverageIndex: 0.82m,
                        WaterSupportIndex: 0.79m,
                        DisruptionRiskIndex: 0.22m,
                        QualityRiskIndex: 0.17m,
                        MaintenancePriorityIndex: 0.35m)
                ]);
        }

        public static CityDistrictPowerDistributionConditionsView CreateCityDistrictPowerDistributionConditionsView(
            Guid cityId)
        {
            return new CityDistrictPowerDistributionConditionsView(
                CityId: cityId,
                EffectiveTickId: 17,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero),
                PowerSupportIndex: 0.77m,
                Districts:
                [
                    new CityDistrictPowerDistributionConditionView(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        PowerCoverageIndex: 0.8m,
                        PowerSupportIndex: 0.77m,
                        OutageRiskIndex: 0.26m,
                        RestorationStrainIndex: 0.19m,
                        MaintenancePriorityIndex: 0.37m)
                ]);
        }

        public static CityDistrictSanitationConditionsView CreateCityDistrictSanitationConditionsView(Guid cityId)
        {
            return new CityDistrictSanitationConditionsView(
                CityId: cityId,
                EffectiveTickId: 17,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero),
                SanitationSupportIndex: 0.74m,
                Districts:
                [
                    new CityDistrictSanitationConditionView(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        SanitationCoverageIndex: 0.78m,
                        SanitationSupportIndex: 0.74m,
                        OverflowRiskIndex: 0.29m,
                        ContaminationRiskIndex: 0.2m,
                        MaintenancePriorityIndex: 0.39m)
                ]);
        }

        public static CityDistrictUtilityIncidentConditionsView CreateCityDistrictUtilityIncidentConditionsView(
            Guid cityId)
        {
            return new CityDistrictUtilityIncidentConditionsView(
                CityId: cityId,
                EffectiveTickId: 17,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero),
                UtilityIncidentSupportIndex: 0.72m,
                Districts:
                [
                    new CityDistrictUtilityIncidentConditionView(
                        DistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                        UtilityContinuityIndex: 0.76m,
                        DispatchReadinessIndex: 0.73m,
                        IncidentPressureIndex: 0.34m,
                        CoordinationDifficultyIndex: 0.24m,
                        RestorationPriorityIndex: 0.41m)
                ]);
        }

        public static CityUtilityIncidentStatusView CreateCityUtilityIncidentStatusView(
            Guid cityId,
            string statusIntensity = "High")
        {
            return new CityUtilityIncidentStatusView(
                CityId: cityId,
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 3,
                    hour: 13,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero),
                UtilityContinuityIndex: 0.68m,
                UtilityIncidentSupportIndex: 0.72m,
                BudgetPressureIndex: 0.33m,
                EmergencyModeEnabled: false,
                DispatchReadinessIndex: 0.71m,
                RestorationCoverageIndex: 0.69m,
                SpareCapacityIndex: 0.62m,
                FieldCoordinationIndex: 0.73m,
                IncidentQueuePressureIndex: 0.38m,
                RequestedIntensity: statusIntensity,
                AppliedIntensity: statusIntensity,
                BudgetAuthorizationStatus: "Authorized",
                BudgetAuthorizationLevel: "Standard",
                BudgetAvailableAmount: 5400m,
                BudgetAuthorizedByEmergencyOverride: false,
                BudgetAuthorizedIntensity: statusIntensity,
                BudgetAuthorizationSummary: "Approved.",
                FocusDistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                PendingOperation: new PendingCityOperationView(
                    Focus: "Balanced",
                    Intensity: statusIntensity,
                    ReadyAtTickId: 19),
                System: new CityUtilityIncidentSystemStatusView(
                    Kind: "PowerDistribution",
                    LoadIndex: 0.48m,
                    ServiceQualityIndex: 0.74m,
                    BacklogIndex: 0.19m,
                    FailureRiskIndex: 0.23m));
        }

        public static DispatchCityResupplyView CreateDispatchCityResupplyView(
            Guid cityId,
            string requestedIntensity = "Medium",
            string? appliedIntensity = "Medium")
        {
            return new DispatchCityResupplyView(
                Status: "Applied",
                CityId: cityId,
                RequestedIntensity: requestedIntensity,
                BudgetAuthorizedIntensity: appliedIntensity,
                AppliedIntensity: appliedIntensity,
                PendingResupply: new PendingResupplyView(
                    Focus: "All",
                    Intensity: appliedIntensity ?? requestedIntensity,
                    FocusDistrictId: Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5"),
                    ReadyAtTickId: 21),
                BudgetPressureIndex: 0.35m,
                BudgetAuthorizationStatus: "Authorized",
                BudgetAuthorizationLevel: "Standard",
                BudgetAvailableAmount: 8300m,
                BudgetAuthorizedByEmergencyOverride: false,
                BudgetAuthorizationSummary: "Approved.",
                SupplyStressIndex: 0.41m,
                FuelStockLevelIndex: 0.62m,
                FoodStockLevelIndex: 0.74m,
                EmergencyWaterStockLevelIndex: 0.58m);
        }

        public static DownstreamServiceException CreateDownstreamServiceException(
            HttpStatusCode statusCode,
            string? body = null,
            string serviceName = "test-service")
        {
            return new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: statusCode,
                body: body,
                contentType: "application/json",
                requestUrl: "https://downstream.example/api");
        }

        public static DownstreamServiceException CreateConflictException<T>(
            T payload,
            string serviceName = "test-service")
            where T : class
        {
            return CreateDownstreamServiceException(
                statusCode: HttpStatusCode.Conflict,
                body: JsonSerializer.Serialize(
                    value: payload,
                    options: new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                serviceName: serviceName);
        }

        private static IEnumerable<Claim> CreateOptionalClaims(string? jti)
        {
            if (!string.IsNullOrWhiteSpace(jti))
                yield return new Claim(
                    type: JwtRegisteredClaimNames.Jti,
                    value: jti);
        }

        private static DashboardMetricView CreateDashboardMetric(
            string label,
            int current)
        {
            return new DashboardMetricView(
                Label: label,
                Current: current,
                Description: $"{label} metric.",
                DeltaYesterday: null,
                DeltaMonth: null,
                DeltaYear: null);
        }

        private static DashboardPeriodComparisonRowView CreateDashboardPeriodRow(string label)
        {
            DashboardWindowComparisonView window = new(
                Current: 1,
                Previous: 0,
                Delta: 1);

            return new DashboardPeriodComparisonRowView(
                Label: label,
                Description: $"{label} comparison.",
                Yesterday: window,
                Month: window,
                Year: window);
        }

        public sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            private readonly DateTimeOffset _utcNow = utcNow;

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }

        public sealed class RecordingClassicCitySetupSessionService : IClassicCitySetupSessionService
        {
            public IReadOnlyList<ClassicCitySetupSessionView> ListDraftsResult { get; set; } = [];
            public ClassicCitySetupSessionView? CreateResult { get; set; }
            public ClassicCitySetupSessionView? GetResult { get; set; }
            public ClassicCitySetupSessionMutationResult? DeleteResult { get; set; }
            public ClassicCitySetupSessionMutationResult? UpdateResult { get; set; }
            public ClassicCitySetupSessionMutationResult? QueueLaunchResult { get; set; }
            public CreateClassicCitySetupSessionRequestDto? LastCreateRequest { get; private set; }
            public Guid? LastGetSessionId { get; private set; }
            public Guid? LastDeleteSessionId { get; private set; }
            public Guid? LastUpdateSessionId { get; private set; }
            public UpdateClassicCitySetupSessionRequestDto? LastUpdateRequest { get; private set; }
            public Guid? LastQueueLaunchSessionId { get; private set; }
            public Guid? LastProcessLaunchSessionId { get; private set; }
            public Guid? LastReconcileSessionId { get; private set; }

            public Task<IReadOnlyList<ClassicCitySetupSessionView>> ListDraftsAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ListDraftsResult);
            }

            public Task<ClassicCitySetupSessionView> CreateAsync(
                CreateClassicCitySetupSessionRequestDto request,
                CancellationToken cancellationToken = default)
            {
                LastCreateRequest = request;

                return Task.FromResult(CreateResult ?? CreateClassicCitySetupSessionView());
            }

            public Task<ClassicCitySetupSessionView?> GetAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                LastGetSessionId = sessionId;
                return Task.FromResult(GetResult);
            }

            public Task<ClassicCitySetupSessionMutationResult> DeleteAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                LastDeleteSessionId = sessionId;

                return Task.FromResult(
                    DeleteResult ??
                    new ClassicCitySetupSessionMutationResult(
                        Status: ClassicCitySetupSessionMutationStatus.Updated,
                        Session: null,
                        ErrorCode: null,
                        ErrorMessage: null));
            }

            public Task<ClassicCitySetupSessionMutationResult> UpdateAsync(
                Guid sessionId,
                UpdateClassicCitySetupSessionRequestDto request,
                CancellationToken cancellationToken = default)
            {
                LastUpdateSessionId = sessionId;
                LastUpdateRequest = request;

                return Task.FromResult(
                    UpdateResult ??
                    new ClassicCitySetupSessionMutationResult(
                        Status: ClassicCitySetupSessionMutationStatus.Updated,
                        Session: CreateClassicCitySetupSessionView(
                            sessionId: sessionId,
                            currentStepId: request.CurrentStepId,
                            draft: request.Draft),
                        ErrorCode: null,
                        ErrorMessage: null));
            }

            public Task<ClassicCitySetupSessionMutationResult> QueueLaunchAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                LastQueueLaunchSessionId = sessionId;

                return Task.FromResult(
                    QueueLaunchResult ??
                    new ClassicCitySetupSessionMutationResult(
                        Status: ClassicCitySetupSessionMutationStatus.Updated,
                        Session: CreateClassicCitySetupSessionView(
                            sessionId: sessionId,
                            status: "LaunchQueued",
                            launchQueuedAtUtc: new DateTimeOffset(
                                year: 2048,
                                month: 6,
                                day: 1,
                                hour: 12,
                                minute: 0,
                                second: 0,
                                offset: TimeSpan.Zero)),
                        ErrorCode: null,
                        ErrorMessage: null));
            }

            public Task ProcessLaunchAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                LastProcessLaunchSessionId = sessionId;
                return Task.CompletedTask;
            }

            public Task ReconcileAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                LastReconcileSessionId = sessionId;
                return Task.CompletedTask;
            }
        }

        public sealed class FakeIdentityInternalUsersClient : IIdentityInternalUsersClient
        {
            public Dictionary<Guid, int> UserPermissionsVersions { get; } = new();
            public Dictionary<Guid, UserAuthContextResponse> UserAuthContexts { get; } = new();
            public int DefaultUserAccessVersion { get; set; } = 1;
            public Exception? GetPermissionsVersionException { get; set; }
            public Exception? GetDefaultUserAccessVersionException { get; set; }
            public Exception? GetAuthContextException { get; set; }
            public int GetPermissionsVersionCallCount { get; private set; }
            public int GetDefaultUserAccessVersionCallCount { get; private set; }
            public int GetAuthContextCallCount { get; private set; }

            public Task<int> GetPermissionsVersionAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                GetPermissionsVersionCallCount++;

                if (GetPermissionsVersionException is not null)
                    throw GetPermissionsVersionException;

                if (!UserPermissionsVersions.TryGetValue(
                        key: userId,
                        value: out int version))
                    throw new KeyNotFoundException($"Permissions version for user '{userId}' was not configured.");

                return Task.FromResult(version);
            }

            public Task<int> GetDefaultUserAccessVersionAsync(CancellationToken cancellationToken)
            {
                GetDefaultUserAccessVersionCallCount++;

                if (GetDefaultUserAccessVersionException is not null)
                    throw GetDefaultUserAccessVersionException;

                return Task.FromResult(DefaultUserAccessVersion);
            }

            public Task<UserAuthContextResponse> GetAuthContextAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                GetAuthContextCallCount++;

                if (GetAuthContextException is not null)
                    throw GetAuthContextException;

                if (!UserAuthContexts.TryGetValue(
                        key: userId,
                        value: out UserAuthContextResponse? context))
                    throw new KeyNotFoundException($"Auth context for user '{userId}' was not configured.");

                return Task.FromResult(context);
            }
        }

        public sealed class FakePermissionsVersionStore : IPermissionsVersionStore
        {
            public int CurrentVersion { get; set; }
            public Exception? Exception { get; set; }
            public int GetCurrentCallCount { get; private set; }
            public Guid? LastRequestedUserId { get; private set; }

            public Task<int> GetCurrentAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                GetCurrentCallCount++;
                LastRequestedUserId = userId;

                if (Exception is not null)
                    throw Exception;

                return Task.FromResult(CurrentVersion);
            }
        }

        public sealed class FakeAuthContextStore : IAuthContextStore
        {
            public Dictionary<(Guid UserId, int PermissionsVersion), UserAuthContextResponse> Responses { get; } =
                new();

            public Exception? Exception { get; set; }
            public int GetCallCount { get; private set; }
            public (Guid UserId, int PermissionsVersion)? LastRequest { get; private set; }

            public Task<UserAuthContextResponse> GetAsync(
                Guid userId,
                int permissionsVersion,
                CancellationToken ct)
            {
                GetCallCount++;
                LastRequest = (userId, permissionsVersion);

                if (Exception is not null)
                    throw Exception;

                if (!Responses.TryGetValue(
                        key: (userId, permissionsVersion),
                        value: out UserAuthContextResponse? response))
                    throw new KeyNotFoundException(
                        $"Auth context for user '{userId}' and version '{permissionsVersion}' was not configured.");

                return Task.FromResult(response);
            }
        }

        public sealed class FakeClassicCitySetupSessionStore : IClassicCitySetupSessionStore
        {
            public Dictionary<Guid, ClassicCitySetupSessionState> Sessions { get; } = new();
            public HashSet<Guid> TrackedSessionIds { get; } = [];
            public List<Guid> UntrackedSessionIds { get; } = [];
            public ClassicCitySetupSessionLockHandle? LockToReturn { get; set; } = new("lock-token");
            public ClassicCitySetupSessionLockHandle? CreateLockToReturn { get; set; } = new("create-lock-token");
            public Exception? TryAcquireLockException { get; set; }
            public Exception? TryAcquireCreateLockException { get; set; }
            public int SaveCallCount { get; private set; }
            public int DeleteCallCount { get; private set; }
            public int ReleaseLockCallCount { get; private set; }
            public int ReleaseCreateLockCallCount { get; private set; }
            public int UntrackCallCount { get; private set; }
            public Guid? LastDeletedSessionId { get; private set; }

            public Task<IReadOnlyList<ClassicCitySetupSessionState>> ListOwnedAsync(
                Guid ownerUserId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<ClassicCitySetupSessionState> sessions = Sessions.Values
                   .Where(x => x.OwnerUserId == ownerUserId)
                   .OrderBy(x => x.SessionId)
                   .ToArray();

                return Task.FromResult(sessions);
            }

            public Task DeleteAsync(
                Guid sessionId,
                Guid? ownerUserId,
                CancellationToken cancellationToken = default)
            {
                DeleteCallCount++;
                LastDeletedSessionId = sessionId;
                Sessions.Remove(sessionId);
                TrackedSessionIds.Remove(sessionId);
                return Task.CompletedTask;
            }

            public Task<ClassicCitySetupSessionState?> GetAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                Sessions.TryGetValue(
                    key: sessionId,
                    value: out ClassicCitySetupSessionState? session);
                return Task.FromResult(session);
            }

            public Task SaveAsync(
                ClassicCitySetupSessionState session,
                CancellationToken cancellationToken = default)
            {
                SaveCallCount++;
                Sessions[session.SessionId] = session;
                return Task.CompletedTask;
            }

            public Task<ClassicCitySetupSessionLockHandle?> TryAcquireLockAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                if (TryAcquireLockException is not null)
                    throw TryAcquireLockException;

                return Task.FromResult(LockToReturn);
            }

            public Task<ClassicCitySetupSessionLockHandle?> TryAcquireCreateLockAsync(
                Guid ownerUserId,
                CancellationToken cancellationToken = default)
            {
                if (TryAcquireCreateLockException is not null)
                    throw TryAcquireCreateLockException;

                return Task.FromResult(CreateLockToReturn);
            }

            public Task ReleaseLockAsync(
                Guid sessionId,
                ClassicCitySetupSessionLockHandle lockHandle,
                CancellationToken cancellationToken = default)
            {
                ReleaseLockCallCount++;
                return Task.CompletedTask;
            }

            public Task ReleaseCreateLockAsync(
                Guid ownerUserId,
                ClassicCitySetupSessionLockHandle lockHandle,
                CancellationToken cancellationToken = default)
            {
                ReleaseCreateLockCallCount++;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<Guid>> ListTrackedSessionIdsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult((IReadOnlyList<Guid>)TrackedSessionIds.ToArray());
            }

            public Task UntrackAsync(
                Guid sessionId,
                CancellationToken cancellationToken = default)
            {
                UntrackCallCount++;
                UntrackedSessionIds.Add(sessionId);
                TrackedSessionIds.Remove(sessionId);
                return Task.CompletedTask;
            }
        }

        public sealed class RecordingProvisioningService(IInternalJwtRequestContextAccessor requestContextAccessor)
            : ICityProvisioningService
        {
            public CityProvisioningView? CreateCityResult { get; set; }
            public Exception? CreateCityException { get; set; }
            public int CreateCityCallCount { get; private set; }
            public CreateCityRequestDto? LastCreateCityRequest { get; private set; }
            public InternalJwtRequestContext? CapturedRequestContext { get; private set; }

            public Task<CityProvisioningView> CreateCityAsync(
                CreateCityRequestDto request,
                CancellationToken cancellationToken = default)
            {
                CreateCityCallCount++;
                LastCreateCityRequest = request;
                CapturedRequestContext = requestContextAccessor.Current;

                if (CreateCityException is not null)
                    throw CreateCityException;

                return Task.FromResult(CreateCityResult ?? CreateCityProvisioningView(Guid.NewGuid()));
            }

            public Task<CityProvisioningView> RetryPopulationBootstrapAsync(
                Guid cityId,
                int? plannedPeopleCountOverride = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("RetryPopulationBootstrapAsync is not used in these tests.");
            }
        }

        public sealed class RecordingCitiesApiClient(IInternalJwtRequestContextAccessor requestContextAccessor)
            : ICitiesApiClient
        {
            public CityProvisioningStatusView? ProvisioningStatusResult { get; set; }
            public Exception? ProvisioningStatusException { get; set; }
            public int GetProvisioningStatusCallCount { get; private set; }
            public Guid? LastProvisioningStatusCityId { get; private set; }
            public InternalJwtRequestContext? CapturedRequestContext { get; private set; }

            public Task<CityCreatedView> CreateCityAsync(
                CreateCityRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityProvisioningView> CreateProvisionedCityAsync(
                CreateCityRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<CityListItemView>> ListCitiesAsync(
                bool includeArchived,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<CityListItemView>> ListProvisioningCitiesAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityView> GetCityAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityProvisioningStatusView> GetProvisioningStatusAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                GetProvisioningStatusCallCount++;
                LastProvisioningStatusCityId = cityId;
                CapturedRequestContext = requestContextAccessor.Current;

                if (ProvisioningStatusException is not null)
                    throw ProvisioningStatusException;

                return Task.FromResult(ProvisioningStatusResult ?? CreateCityProvisioningStatusView(cityId));
            }

            public Task<CityWeatherView> GetWeatherAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityMapTopologyView> GetMapAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<ResidentialBuildingView>> GetResidentialBuildingsAsync(
                Guid cityId,
                Guid? districtId = null,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationBootstrapRestartedView> RestartPopulationBootstrapAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityProvisioningView> RetryPopulationBootstrapProvisioningAsync(
                Guid cityId,
                RetryCityPopulationBootstrapProvisioningRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task CompletePopulationBootstrapAsync(
                Guid cityId,
                CompleteCityPopulationBootstrapRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task CompleteEconomyBootstrapAsync(
                Guid cityId,
                CompleteCityEconomyBootstrapRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task FailPopulationBootstrapAsync(
                Guid cityId,
                FailCityPopulationBootstrapRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task FailEconomyBootstrapAsync(
                Guid cityId,
                FailCityEconomyBootstrapRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task UpdateEnvironmentAsync(
                Guid cityId,
                UpdateCityEnvironmentRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task RenameCityAsync(
                Guid cityId,
                RenameCityRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task ArchiveCityAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteCityAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class RecordingPublishEndpoint : IPublishEndpoint
        {
            public List<object> PublishedMessages { get; } = [];
            public Exception? Exception { get; set; }

            public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
            {
                return new NoOpConnectHandle();
            }

            public Task Publish<T>(
                T message,
                CancellationToken cancellationToken = default)
                where T : class
            {
                if (Exception is not null)
                    throw Exception;

                PublishedMessages.Add(message);
                return Task.CompletedTask;
            }

            public Task Publish<T>(
                T message,
                IPipe<PublishContext<T>> publishPipe,
                CancellationToken cancellationToken = default)
                where T : class
            {
                return Publish(
                    message: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish<T>(
                T message,
                IPipe<PublishContext> publishPipe,
                CancellationToken cancellationToken = default)
                where T : class
            {
                return Publish(
                    message: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish(
                object message,
                CancellationToken cancellationToken = default)
            {
                return Publish<object>(
                    values: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish(
                object message,
                Type messageType,
                CancellationToken cancellationToken = default)
            {
                return Publish(
                    message: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish(
                object message,
                IPipe<PublishContext> publishPipe,
                CancellationToken cancellationToken = default)
            {
                return Publish(
                    message: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish(
                object message,
                Type messageType,
                IPipe<PublishContext> publishPipe,
                CancellationToken cancellationToken = default)
            {
                return Publish(
                    message: message,
                    cancellationToken: cancellationToken);
            }

            public Task Publish<T>(
                object values,
                CancellationToken cancellationToken = default)
                where T : class
            {
                return Publish(
                    message: values,
                    cancellationToken: cancellationToken);
            }

            public Task Publish<T>(
                object values,
                IPipe<PublishContext<T>> publishPipe,
                CancellationToken cancellationToken = default)
                where T : class
            {
                return Publish(
                    message: values,
                    cancellationToken: cancellationToken);
            }

            public Task Publish<T>(
                object values,
                IPipe<PublishContext> publishPipe,
                CancellationToken cancellationToken = default)
                where T : class
            {
                return Publish(
                    message: values,
                    cancellationToken: cancellationToken);
            }

            private sealed class NoOpConnectHandle : ConnectHandle
            {
                public void Dispose() { }

                public void Disconnect() { }
            }
        }

        public sealed class RecordingSimulationApiClient : ISimulationApiClient
        {
            public SimulationClockView? ClockResult { get; set; }
            public int GetClockCallCount { get; private set; }
            public Guid? LastClockSimulationId { get; private set; }
            public Guid? LastPausedSimulationId { get; private set; }
            public Guid? LastResumedSimulationId { get; private set; }
            public Guid? LastSetSpeedSimulationId { get; private set; }
            public SetSpeedRequest? LastSetSpeedRequest { get; private set; }
            public Guid? LastJumpSimulationId { get; private set; }
            public JumpClockRequest? LastJumpRequest { get; private set; }

            public Task<SimulationClockView> GetClockAsync(
                Guid simulationId,
                CancellationToken cancellationToken = default)
            {
                GetClockCallCount++;
                LastClockSimulationId = simulationId;

                return Task.FromResult(ClockResult ?? CreateSimulationClockView(simulationId: simulationId));
            }

            public Task PauseClockAsync(
                Guid simulationId,
                CancellationToken cancellationToken = default)
            {
                LastPausedSimulationId = simulationId;
                return Task.CompletedTask;
            }

            public Task ResumeClockAsync(
                Guid simulationId,
                CancellationToken cancellationToken = default)
            {
                LastResumedSimulationId = simulationId;
                return Task.CompletedTask;
            }

            public Task SetClockSpeedAsync(
                Guid simulationId,
                SetSpeedRequest request,
                CancellationToken cancellationToken = default)
            {
                LastSetSpeedSimulationId = simulationId;
                LastSetSpeedRequest = request;
                return Task.CompletedTask;
            }

            public Task JumpClockAsync(
                Guid simulationId,
                JumpClockRequest request,
                CancellationToken cancellationToken = default)
            {
                LastJumpSimulationId = simulationId;
                LastJumpRequest = request;
                return Task.CompletedTask;
            }
        }

        public sealed class RecordingCityOperationsDashboardService : ICityOperationsDashboardService
        {
            public CityOperationsDashboardView? View { get; set; }
            public int GetCallCount { get; private set; }

            public Task<CityOperationsDashboardView> GetAsync(CancellationToken cancellationToken)
            {
                GetCallCount++;
                return Task.FromResult(View ?? CreateCityOperationsDashboardView());
            }
        }

        public sealed class RecordingTripsApiClient : ITripsApiClient
        {
            public IReadOnlyList<CityActiveTripView> Result { get; set; } = [];
            public Exception? Exception { get; set; }
            public Guid? LastCityId { get; private set; }
            public int CallCount { get; private set; }

            public Task<IReadOnlyList<CityActiveTripView>> GetActiveTripsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastCityId = cityId;

                if (Exception is not null)
                    throw Exception;

                return Task.FromResult(Result);
            }
        }

        public sealed class RecordingEconomyApiClient : IClassicCityEconomyApiClient
        {
            public EconomySummaryView? CitySummaryResult { get; set; }
            public CityOperationalBudgetPressureView? BudgetPressureResult { get; set; }
            public Exception? GetCitySummaryException { get; set; }
            public Exception? BudgetPressureException { get; set; }
            public int GetCitySummaryCallCount { get; private set; }
            public int BudgetPressureCallCount { get; private set; }
            public Guid? LastCitySummaryCityId { get; private set; }
            public Guid? LastBudgetPressureCityId { get; private set; }

            public Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<EconomySummaryView?> GetCitySummaryAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                GetCitySummaryCallCount++;
                LastCitySummaryCityId = cityId;

                if (GetCitySummaryException is not null)
                    throw GetCitySummaryException;

                return Task.FromResult(CitySummaryResult);
            }

            public Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                BudgetPressureCallCount++;
                LastBudgetPressureCityId = cityId;

                if (BudgetPressureException is not null)
                    throw BudgetPressureException;

                return Task.FromResult(BudgetPressureResult);
            }

            public Task<IReadOnlyList<CityBusinessView>> GetCityBusinessesAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IReadOnlyList<CityHouseholdAccountView>> GetCityHouseholdAccountsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CursorPagedResult<BudgetLedgerEntryView>> GetCityBudgetLedgerFeedAsync(
                Guid cityId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CursorPagedResult<CityBusinessLedgerEntryView>> GetCityBusinessLedgerFeedAsync(
                Guid businessId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CursorPagedResult<CityHouseholdAccountLedgerEntryView>> GetCityHouseholdAccountLedgerFeedAsync(
                Guid householdAccountId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(
                Guid cityId,
                InitializeCityEconomyRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class RecordingPopulationApiClient : IPopulationApiClient, IClassicCityPopulationApiClient
        {
            public CityPopulationDashboardDto? DashboardResult { get; set; }
            public CityPopulationDistrictPressureDto? DistrictPressureResult { get; set; }
            public PagedResult<PersonDto>? ResidentsPageResult { get; set; }
            public CityResidentDetailsDto? ResidentDetailsResult { get; set; }
            public Exception? DistrictPressureException { get; set; }
            public Guid? LastDashboardCityId { get; private set; }
            public Guid? LastDistrictPressureCityId { get; private set; }
            public Guid? LastResidentsPageCityId { get; private set; }
            public DateOnly? LastResidentsPageCurrentDate { get; private set; }
            public int? LastResidentsPageNumber { get; private set; }
            public int? LastResidentsPageSize { get; private set; }
            public Guid? LastResidentDetailsCityId { get; private set; }
            public Guid? LastResidentDetailsPersonId { get; private set; }
            public DateOnly? LastResidentDetailsCurrentDate { get; private set; }
            public int DistrictPressureCallCount { get; private set; }

            public Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
                InitializeCityPopulationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationDashboardDto> GetCityPopulationDashboardAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                LastDashboardCityId = cityId;
                return Task.FromResult(DashboardResult ?? CreateCityPopulationDashboardDto(cityId));
            }

            public Task<CityPopulationDistrictPressureDto> GetCityDistrictPressureAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                DistrictPressureCallCount++;
                LastDistrictPressureCityId = cityId;

                if (DistrictPressureException is not null)
                    throw DistrictPressureException;

                return Task.FromResult(DistrictPressureResult ?? CreateCityPopulationDistrictPressureDto(cityId));
            }

            public Task<PagedResult<PersonDto>> GetCityResidentsPageAsync(
                Guid cityId,
                DateOnly currentDate,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                LastResidentsPageCityId = cityId;
                LastResidentsPageCurrentDate = currentDate;
                LastResidentsPageNumber = pageNumber;
                LastResidentsPageSize = pageSize;

                return Task.FromResult(ResidentsPageResult ?? CreateResidentsPageResult());
            }

            public Task<CityResidentDetailsDto> GetCityResidentDetailsAsync(
                Guid cityId,
                Guid personId,
                DateOnly currentDate,
                CancellationToken cancellationToken = default)
            {
                LastResidentDetailsCityId = cityId;
                LastResidentDetailsPersonId = personId;
                LastResidentDetailsCurrentDate = currentDate;

                return Task.FromResult(ResidentDetailsResult ?? CreateCityResidentDetailsDto(personId));
            }

            public Task<CityEmploymentCatalogDto> GetCityEmploymentCatalogAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationCatalogDto> GetCityEducationCatalogAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> HireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> FireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> RetireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> EnrollCityResidentAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> GraduateCityResidentAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> WithdrawCityResidentFromStudyAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityCivilRegistryOperationResultDto> RegisterCityMarriageAsync(
                Guid cityId,
                CityCivilRegistryOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityCivilRegistryOperationResultDto> RegisterCityDivorceAsync(
                Guid cityId,
                CityCivilRegistryOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<PagedResult<PersonDto>> GetPeoplePageAsync(
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class RecordingEducationApiClient : IEducationApiClient
        {
            public EducationInstitutionCatalogResponse CatalogResult { get; set; } = new([]);
            public EducationEnrollmentOperationResponse EnrollResult { get; set; } = new("Applied");
            public EducationEnrollmentOperationResponse CompleteResult { get; set; } = new("Applied");
            public EducationEnrollmentOperationResponse WithdrawResult { get; set; } = new("Applied");
            public StudentEducationStatusResponse? StudentStatusResult { get; set; }
            public Guid? LastSimulationHostId { get; private set; }
            public Guid? LastStudentStatusResidentId { get; private set; }
            public EnrollStudentRequest? LastEnrollRequest { get; private set; }
            public CompleteStudentStageRequest? LastCompleteRequest { get; private set; }
            public WithdrawStudentRequest? LastWithdrawRequest { get; private set; }

            public Task<EducationInstitutionCatalogResponse> ListInstitutionsAsync(
                Guid simulationHostId,
                CancellationToken cancellationToken = default)
            {
                LastSimulationHostId = simulationHostId;
                return Task.FromResult(CatalogResult);
            }

            public Task<SynchronizeEducationInstitutionsResponse> SynchronizeInstitutionsAsync(
                Guid simulationHostId,
                SynchronizeEducationInstitutionsRequest request,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<StudentEducationStatusResponse?> GetStudentStatusAsync(
                Guid simulationHostId,
                Guid residentId,
                CancellationToken cancellationToken = default)
            {
                LastSimulationHostId = simulationHostId;
                LastStudentStatusResidentId = residentId;
                return Task.FromResult(StudentStatusResult);
            }

            public Task<EducationEnrollmentOperationResponse> EnrollStudentAsync(
                Guid simulationHostId,
                EnrollStudentRequest request,
                CancellationToken cancellationToken = default)
            {
                LastSimulationHostId = simulationHostId;
                LastEnrollRequest = request;
                return Task.FromResult(EnrollResult);
            }

            public Task<EducationEnrollmentOperationResponse> CompleteStudentStageAsync(
                Guid simulationHostId,
                CompleteStudentStageRequest request,
                CancellationToken cancellationToken = default)
            {
                LastSimulationHostId = simulationHostId;
                LastCompleteRequest = request;
                return Task.FromResult(CompleteResult);
            }

            public Task<EducationEnrollmentOperationResponse> WithdrawStudentAsync(
                Guid simulationHostId,
                WithdrawStudentRequest request,
                CancellationToken cancellationToken = default)
            {
                LastSimulationHostId = simulationHostId;
                LastWithdrawRequest = request;
                return Task.FromResult(WithdrawResult);
            }
        }

        public sealed class RecordingStockpilesApiClient : IStockpilesApiClient
        {
            public CityStockpilesView? StockpilesResult { get; set; }
            public Exception? StockpilesException { get; set; }
            public DispatchCityResupplyView? DispatchResult { get; set; }
            public Exception? DispatchException { get; set; }
            public Guid? LastStockpilesCityId { get; private set; }
            public Guid? LastDispatchCityId { get; private set; }
            public DispatchCityResupplyRequest? LastDispatchRequest { get; private set; }
            public int StockpilesCallCount { get; private set; }

            public Task<CityStockpilesView?> GetCityStockpilesAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                StockpilesCallCount++;
                LastStockpilesCityId = cityId;

                if (StockpilesException is not null)
                    throw StockpilesException;

                return Task.FromResult(StockpilesResult);
            }

            public Task<DispatchCityResupplyView> DispatchCityResupplyAsync(
                Guid cityId,
                DispatchCityResupplyRequest request,
                CancellationToken cancellationToken = default)
            {
                LastDispatchCityId = cityId;
                LastDispatchRequest = request;

                if (DispatchException is not null)
                    throw DispatchException;

                return Task.FromResult(DispatchResult ?? CreateDispatchCityResupplyView(cityId));
            }
        }

        public sealed class RecordingEnvironmentalConditionsApiClient : IEnvironmentalConditionsApiClient
        {
            public bool ReturnHeatingNull { get; set; }
            public bool ReturnWaterNull { get; set; }
            public bool ReturnPowerNull { get; set; }
            public bool ReturnSanitationNull { get; set; }
            public bool ReturnUtilityIncidentNull { get; set; }
            public CityEnvironmentalConditionsView? ConditionsResult { get; set; }
            public CityDistrictHeatingConditionsView? HeatingResult { get; set; }
            public CityDistrictWaterDistributionConditionsView? WaterResult { get; set; }
            public CityDistrictPowerDistributionConditionsView? PowerResult { get; set; }
            public CityDistrictSanitationConditionsView? SanitationResult { get; set; }
            public CityDistrictUtilityIncidentConditionsView? UtilityIncidentResult { get; set; }
            public Exception? ConditionsException { get; set; }
            public Exception? HeatingException { get; set; }
            public Exception? WaterException { get; set; }
            public Exception? PowerException { get; set; }
            public Exception? SanitationException { get; set; }
            public Exception? UtilityIncidentException { get; set; }
            public CityUtilityIncidentStatusView? DispatchResult { get; set; }
            public Exception? DispatchException { get; set; }
            public Guid? LastConditionsCityId { get; private set; }
            public Guid? LastHeatingCityId { get; private set; }
            public Guid? LastWaterCityId { get; private set; }
            public Guid? LastPowerCityId { get; private set; }
            public Guid? LastSanitationCityId { get; private set; }
            public Guid? LastUtilityIncidentCityId { get; private set; }
            public Guid? LastDispatchCityId { get; private set; }
            public DispatchCityUtilityIncidentResponseRequest? LastDispatchRequest { get; private set; }
            public int ConditionsCallCount { get; private set; }
            public int HeatingCallCount { get; private set; }
            public int WaterCallCount { get; private set; }
            public int PowerCallCount { get; private set; }
            public int SanitationCallCount { get; private set; }
            public int UtilityIncidentCallCount { get; private set; }

            public Task<CityEnvironmentalConditionsView?> GetCityEnvironmentalConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                ConditionsCallCount++;
                LastConditionsCityId = cityId;

                if (ConditionsException is not null)
                    throw ConditionsException;

                return Task.FromResult(ConditionsResult);
            }

            public Task<CityDistrictHeatingConditionsView?> GetCityDistrictHeatingConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                HeatingCallCount++;
                LastHeatingCityId = cityId;

                if (HeatingException is not null)
                    throw HeatingException;

                if (ReturnHeatingNull)
                    return Task.FromResult<CityDistrictHeatingConditionsView?>(null);

                return Task.FromResult<CityDistrictHeatingConditionsView?>(
                    HeatingResult ?? CreateCityDistrictHeatingConditionsView(cityId));
            }

            public Task<CityDistrictWaterDistributionConditionsView?> GetCityDistrictWaterDistributionConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                WaterCallCount++;
                LastWaterCityId = cityId;

                if (WaterException is not null)
                    throw WaterException;

                if (ReturnWaterNull)
                    return Task.FromResult<CityDistrictWaterDistributionConditionsView?>(null);

                return Task.FromResult<CityDistrictWaterDistributionConditionsView?>(
                    WaterResult ?? CreateCityDistrictWaterDistributionConditionsView(cityId));
            }

            public Task<CityDistrictPowerDistributionConditionsView?> GetCityDistrictPowerDistributionConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                PowerCallCount++;
                LastPowerCityId = cityId;

                if (PowerException is not null)
                    throw PowerException;

                if (ReturnPowerNull)
                    return Task.FromResult<CityDistrictPowerDistributionConditionsView?>(null);

                return Task.FromResult<CityDistrictPowerDistributionConditionsView?>(
                    PowerResult ?? CreateCityDistrictPowerDistributionConditionsView(cityId));
            }

            public Task<CityDistrictSanitationConditionsView?> GetCityDistrictSanitationConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                SanitationCallCount++;
                LastSanitationCityId = cityId;

                if (SanitationException is not null)
                    throw SanitationException;

                if (ReturnSanitationNull)
                    return Task.FromResult<CityDistrictSanitationConditionsView?>(null);

                return Task.FromResult<CityDistrictSanitationConditionsView?>(
                    SanitationResult ?? CreateCityDistrictSanitationConditionsView(cityId));
            }

            public Task<CityDistrictUtilityIncidentConditionsView?> GetCityDistrictUtilityIncidentConditionsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                UtilityIncidentCallCount++;
                LastUtilityIncidentCityId = cityId;

                if (UtilityIncidentException is not null)
                    throw UtilityIncidentException;

                if (ReturnUtilityIncidentNull)
                    return Task.FromResult<CityDistrictUtilityIncidentConditionsView?>(null);

                return Task.FromResult<CityDistrictUtilityIncidentConditionsView?>(
                    UtilityIncidentResult ?? CreateCityDistrictUtilityIncidentConditionsView(cityId));
            }

            public Task<CityUtilityIncidentStatusView> DispatchCityUtilityIncidentResponseAsync(
                Guid cityId,
                DispatchCityUtilityIncidentResponseRequest request,
                CancellationToken cancellationToken = default)
            {
                LastDispatchCityId = cityId;
                LastDispatchRequest = request;

                if (DispatchException is not null)
                    throw DispatchException;

                return Task.FromResult(DispatchResult ?? CreateCityUtilityIncidentStatusView(cityId));
            }
        }

        public sealed class RecordingDistributedCache : IDistributedCache
        {
            private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

            public Dictionary<string, DistributedCacheEntryOptions> WrittenOptions { get; } =
                new(StringComparer.Ordinal);

            public Exception? GetException { get; set; }
            public Exception? SetException { get; set; }

            public byte[]? Get(string key)
            {
                return _entries.TryGetValue(
                    key: key,
                    value: out byte[]? value)
                    ? value.ToArray()
                    : null;
            }

            public Task<byte[]?> GetAsync(
                string key,
                CancellationToken token = default)
            {
                if (GetException is not null)
                    throw GetException;

                return Task.FromResult(Get(key));
            }

            public void Refresh(string key) { }

            public Task RefreshAsync(
                string key,
                CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public void Remove(string key)
            {
                _entries.Remove(key);
                WrittenOptions.Remove(key);
            }

            public Task RemoveAsync(
                string key,
                CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }

            public void Set(
                string key,
                byte[] value,
                DistributedCacheEntryOptions options)
            {
                _entries[key] = value.ToArray();
                WrittenOptions[key] = CloneOptions(options);
            }

            public Task SetAsync(
                string key,
                byte[] value,
                DistributedCacheEntryOptions options,
                CancellationToken token = default)
            {
                if (SetException is not null)
                    throw SetException;

                Set(
                    key: key,
                    value: value,
                    options: options);

                return Task.CompletedTask;
            }

            public void SeedString(
                string key,
                string value)
            {
                _entries[key] = Encoding.UTF8.GetBytes(value);
            }

            public string? ReadString(string key)
            {
                return _entries.TryGetValue(
                    key: key,
                    value: out byte[]? value)
                    ? Encoding.UTF8.GetString(value)
                    : null;
            }

            private static DistributedCacheEntryOptions CloneOptions(DistributedCacheEntryOptions options)
            {
                return new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = options.AbsoluteExpiration,
                    AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                    SlidingExpiration = options.SlidingExpiration
                };
            }
        }
    }
}
