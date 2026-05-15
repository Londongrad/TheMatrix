using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Matrix.SimulationSystems.Api.Tests.TestSupport;

public static class SimulationSystemsApiTestSupport
{
    private static readonly Guid DefaultCityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
    private static readonly Guid DefaultDistrictId = Guid.Parse("6fb5e8ef-8c60-40c9-84d8-eb4dc88ea325");
    private static readonly Guid DefaultFocusDistrictId = Guid.Parse("b6130689-a065-4cf3-902a-d26a96756493");
    private static readonly Guid DefaultRoadNodeId = Guid.Parse("f8c53494-c4f2-4674-95f9-9d8e6f6bad4a");
    private static readonly Guid DefaultRoadNodeTargetId = Guid.Parse("82c6f344-2206-4fd7-8c1f-a86f84d3b3c0");
    private static readonly Guid DefaultRoadSegmentId = Guid.Parse("0a05cd9e-a9a6-4812-abf3-58f7089539e5");
    private static readonly DateTimeOffset LastEvaluatedAtUtc = new(2051, 7, 11, 9, 45, 0, TimeSpan.Zero);

    public static CityEnvironmentalConditionsDto CreateEnvironmentalConditionsDto(Guid? cityId = null)
    {
        return new CityEnvironmentalConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            FloodingIndex: 0.44m,
            SnowAccumulationIndex: 0.18m,
            RoadAccessibilityIndex: 0.77m,
            PowerCoverageIndex: 0.86m,
            UtilityContinuityIndex: 0.83m,
            HeatingCoverageIndex: 0.79m,
            WaterCoverageIndex: 0.82m,
            SanitationCoverageIndex: 0.81m,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            ResourceSupply: CreateResourceSupplyConditionDto(),
            Drainage: CreateSystemConditionDto("Drainage"),
            SnowRemoval: CreateSystemConditionDto("SnowRemoval"),
            RoadAccess: CreateSystemConditionDto("RoadAccess"),
            PowerDistribution: CreateSystemConditionDto("PowerDistribution"),
            UtilityIncidents: CreateSystemConditionDto("UtilityIncidents"),
            Heating: CreateSystemConditionDto("Heating"),
            WaterDistribution: CreateSystemConditionDto("WaterDistribution"),
            Sanitation: CreateSystemConditionDto("Sanitation"));
    }

    public static CityDrainageStatusDto CreateDrainageStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CityDrainageStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            FloodingIndex: 0.44m,
            DrainageSupportIndex: 0.68m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            PumpCapacityIndex: 0.74m,
            NetworkIntegrityIndex: 0.81m,
            BlockageIndex: 0.27m,
            CrewReadinessIndex: 0.79m,
            IncidentPressureIndex: 0.18m,
            RequestedIntensity: "Elevated",
            AppliedIntensity: "Baseline",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1250m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Baseline",
            BudgetAuthorizationSummary: "Drainage authorization summary",
            PendingOperation: CreatePendingOperationDto(),
            System: new CityDrainageSystemStatusDto("Drainage", 0.36m, 0.84m, 0.18m, 0.12m));
    }

    public static CityHeatingStatusDto CreateHeatingStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CityHeatingStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            HeatingCoverageIndex: 0.79m,
            HeatingSupportIndex: 0.73m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            PlantCapacityIndex: 0.88m,
            NetworkIntegrityIndex: 0.81m,
            ControlReadinessIndex: 0.76m,
            CrewReadinessIndex: 0.72m,
            IncidentPressureIndex: 0.19m,
            RequestedIntensity: "Focused",
            AppliedIntensity: "Focused",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1480m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Focused",
            BudgetAuthorizationSummary: "Heating authorization summary",
            PendingOperation: CreatePendingOperationDto("Boilers", "Focused", 43),
            System: new CityHeatingSystemStatusDto("Heating", 0.33m, 0.87m, 0.15m, 0.10m));
    }

    public static CityDistrictHeatingConditionsDto CreateHeatingDistrictConditionsDto(Guid? cityId = null)
    {
        return new CityDistrictHeatingConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            HeatingSupportIndex: 0.73m,
            Districts:
            [
                new CityDistrictHeatingConditionDto(
                    DistrictId: DefaultDistrictId,
                    HeatingCoverageIndex: 0.78m,
                    HeatingSupportIndex: 0.74m,
                    OutageRiskIndex: 0.12m,
                    ComfortStressIndex: 0.26m,
                    MaintenancePriorityIndex: 0.57m)
            ]);
    }

    public static CityWaterDistributionStatusDto CreateWaterDistributionStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CityWaterDistributionStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            WaterCoverageIndex: 0.82m,
            WaterSupportIndex: 0.75m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            TreatmentCapacityIndex: 0.84m,
            NetworkIntegrityIndex: 0.79m,
            PumpReadinessIndex: 0.80m,
            CrewReadinessIndex: 0.77m,
            IncidentPressureIndex: 0.15m,
            RequestedIntensity: "Elevated",
            AppliedIntensity: "Elevated",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1630m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Elevated",
            BudgetAuthorizationSummary: "Water authorization summary",
            PendingOperation: CreatePendingOperationDto("Treatment", "Elevated", 44),
            System: new CityWaterDistributionSystemStatusDto("WaterDistribution", 0.35m, 0.86m, 0.14m, 0.09m));
    }

    public static CityDistrictWaterDistributionConditionsDto CreateWaterDistrictConditionsDto(Guid? cityId = null)
    {
        return new CityDistrictWaterDistributionConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            WaterSupportIndex: 0.75m,
            Districts:
            [
                new CityDistrictWaterDistributionConditionDto(
                    DistrictId: DefaultDistrictId,
                    WaterCoverageIndex: 0.83m,
                    WaterSupportIndex: 0.75m,
                    DisruptionRiskIndex: 0.11m,
                    QualityRiskIndex: 0.16m,
                    MaintenancePriorityIndex: 0.49m)
            ]);
    }

    public static CitySanitationStatusDto CreateSanitationStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CitySanitationStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            SanitationCoverageIndex: 0.81m,
            SanitationSupportIndex: 0.70m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            TreatmentStabilityIndex: 0.76m,
            NetworkIntegrityIndex: 0.78m,
            OverflowControlIndex: 0.74m,
            CrewReadinessIndex: 0.75m,
            IncidentPressureIndex: 0.22m,
            RequestedIntensity: "Stabilize",
            AppliedIntensity: "Stabilize",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1380m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Stabilize",
            BudgetAuthorizationSummary: "Sanitation authorization summary",
            PendingOperation: CreatePendingOperationDto("Overflow", "Stabilize", 45),
            System: new CitySanitationSystemStatusDto("Sanitation", 0.38m, 0.82m, 0.21m, 0.14m));
    }

    public static CityDistrictSanitationConditionsDto CreateSanitationDistrictConditionsDto(Guid? cityId = null)
    {
        return new CityDistrictSanitationConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            SanitationSupportIndex: 0.70m,
            Districts:
            [
                new CityDistrictSanitationConditionDto(
                    DistrictId: DefaultDistrictId,
                    SanitationCoverageIndex: 0.80m,
                    SanitationSupportIndex: 0.70m,
                    OverflowRiskIndex: 0.19m,
                    ContaminationRiskIndex: 0.14m,
                    MaintenancePriorityIndex: 0.54m)
            ]);
    }

    public static CityPowerDistributionStatusDto CreatePowerDistributionStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CityPowerDistributionStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            PowerCoverageIndex: 0.86m,
            PowerSupportIndex: 0.77m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            SubstationCapacityIndex: 0.88m,
            GridIntegrityIndex: 0.83m,
            SwitchingReadinessIndex: 0.79m,
            CrewReadinessIndex: 0.73m,
            IncidentPressureIndex: 0.17m,
            RequestedIntensity: "Elevated",
            AppliedIntensity: "Elevated",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Protected",
            BudgetAvailableAmount: 1725m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Elevated",
            BudgetAuthorizationSummary: "Power authorization summary",
            PendingOperation: CreatePendingOperationDto("Substations", "Elevated", 46),
            System: new CityPowerDistributionSystemStatusDto("PowerDistribution", 0.29m, 0.90m, 0.13m, 0.08m));
    }

    public static CityDistrictPowerDistributionConditionsDto CreatePowerDistrictConditionsDto(Guid? cityId = null)
    {
        return new CityDistrictPowerDistributionConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            PowerSupportIndex: 0.77m,
            Districts:
            [
                new CityDistrictPowerDistributionConditionDto(
                    DistrictId: DefaultDistrictId,
                    PowerCoverageIndex: 0.87m,
                    PowerSupportIndex: 0.77m,
                    OutageRiskIndex: 0.10m,
                    RestorationStrainIndex: 0.16m,
                    MaintenancePriorityIndex: 0.43m)
            ]);
    }

    public static CitySnowRemovalStatusDto CreateSnowRemovalStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CitySnowRemovalStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            SnowAccumulationIndex: 0.18m,
            RoadAccessibilityIndex: 0.77m,
            SnowRemovalSupportIndex: 0.72m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            FleetAvailabilityIndex: 0.81m,
            RouteCoverageIndex: 0.78m,
            DeicingReadinessIndex: 0.76m,
            CrewReadinessIndex: 0.74m,
            IncidentPressureIndex: 0.11m,
            RequestedIntensity: "Focused",
            AppliedIntensity: "Focused",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1195m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Focused",
            BudgetAuthorizationSummary: "Snow authorization summary",
            PendingOperation: CreatePendingOperationDto("Routes", "Focused", 47),
            System: new CitySnowRemovalSystemStatusDto("SnowRemoval", 0.32m, 0.85m, 0.17m, 0.11m));
    }

    public static CityRoadAccessStatusDto CreateRoadAccessStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved")
    {
        return new CityRoadAccessStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            FloodingIndex: 0.44m,
            SnowAccumulationIndex: 0.18m,
            RoadAccessibilityIndex: 0.77m,
            RoadSupportIndex: 0.74m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            CorridorAvailabilityIndex: 0.79m,
            SurfaceIntegrityIndex: 0.76m,
            TrafficControlReadinessIndex: 0.80m,
            CrewReadinessIndex: 0.78m,
            IncidentPressureIndex: 0.20m,
            RequestedIntensity: "Stabilize",
            AppliedIntensity: "Stabilize",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Managed",
            BudgetAvailableAmount: 1440m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Stabilize",
            BudgetAuthorizationSummary: "Road authorization summary",
            PendingOperation: CreatePendingOperationDto("Corridors", "Stabilize", 48),
            System: new CityRoadAccessSystemStatusDto("RoadAccess", 0.34m, 0.83m, 0.18m, 0.13m));
    }

    public static CityRoadSegmentConditionsDto CreateRoadSegmentConditionsDto(Guid? cityId = null)
    {
        return new CityRoadSegmentConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            RoadSupportIndex: 0.74m,
            Segments:
            [
                new CityRoadSegmentConditionDto(
                    RoadSegmentId: DefaultRoadSegmentId,
                    DistrictId: DefaultDistrictId,
                    FromRoadNodeId: DefaultRoadNodeId,
                    ToRoadNodeId: DefaultRoadNodeTargetId,
                    Name: "Central Connector",
                    Type: "Collector",
                    LengthMeters: 180m,
                    PassabilityIndex: 0.78m,
                    SpeedMultiplierIndex: 0.81m,
                    SlipRiskIndex: 0.09m,
                    ClosureRiskIndex: 0.06m,
                    MaintenancePriorityIndex: 0.58m)
            ]);
    }

    public static CityUtilityIncidentStatusDto CreateUtilityIncidentStatusDto(
        Guid? cityId = null,
        bool emergencyModeEnabled = false,
        string? budgetAuthorizationStatus = "Approved",
        Guid? focusDistrictId = null)
    {
        return new CityUtilityIncidentStatusDto(
            CityId: cityId ?? DefaultCityId,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            UtilityContinuityIndex: 0.83m,
            UtilityIncidentSupportIndex: 0.69m,
            BudgetPressureIndex: 0.31m,
            EmergencyModeEnabled: emergencyModeEnabled,
            DispatchReadinessIndex: 0.80m,
            RestorationCoverageIndex: 0.75m,
            SpareCapacityIndex: 0.71m,
            FieldCoordinationIndex: 0.73m,
            IncidentQueuePressureIndex: 0.24m,
            RequestedIntensity: "Rapid",
            AppliedIntensity: "Rapid",
            BudgetAuthorizationStatus: budgetAuthorizationStatus,
            BudgetAuthorizationLevel: "Protected",
            BudgetAvailableAmount: 2100m,
            BudgetAuthorizedByEmergencyOverride: false,
            BudgetAuthorizedIntensity: "Rapid",
            BudgetAuthorizationSummary: "Utility incident authorization summary",
            FocusDistrictId: focusDistrictId ?? DefaultFocusDistrictId,
            PendingOperation: CreatePendingOperationDto("Restoration", "Rapid", 49),
            System: new CityUtilityIncidentSystemStatusDto("UtilityIncidents", 0.37m, 0.82m, 0.23m, 0.16m));
    }

    public static CityDistrictUtilityIncidentConditionsDto CreateUtilityIncidentDistrictConditionsDto(Guid? cityId = null)
    {
        return new CityDistrictUtilityIncidentConditionsDto(
            CityId: cityId ?? DefaultCityId,
            EffectiveTickId: 42,
            LastEvaluatedAtUtc: LastEvaluatedAtUtc,
            UtilityIncidentSupportIndex: 0.69m,
            Districts:
            [
                new CityDistrictUtilityIncidentConditionDto(
                    DistrictId: DefaultDistrictId,
                    UtilityContinuityIndex: 0.81m,
                    DispatchReadinessIndex: 0.77m,
                    IncidentPressureIndex: 0.22m,
                    CoordinationDifficultyIndex: 0.16m,
                    RestorationPriorityIndex: 0.61m)
            ]);
    }

    public static WebApplicationBuilder CreateBuilder(IConfiguration? configuration = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        if (configuration is not null)
        {
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddConfiguration(configuration);
        }

        return builder;
    }

    public static IConfiguration BuildValidApiConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["ConnectionStrings:SimulationSystemsDb"] = "Host=localhost;Port=5432;Database=simulationsystems_tests;Username=postgres;Password=postgres",
            ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
            ["InternalUserContextJwt:Audience"] = "simulationsystems-api",
            ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
            ["InternalUserContextJwt:LifetimeSeconds"] = "300",
            ["InternalServiceJwt:Issuer"] = "https://gateway.test",
            ["InternalServiceJwt:Audience"] = "simulationsystems-api",
            ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
            ["InternalServiceJwt:LifetimeSeconds"] = "300",
            ["RabbitMq:Host"] = "rabbitmq.test",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:VirtualHost"] = "/",
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
            ["DownstreamServices:Economy"] = "https://economy.test",
            ["DownstreamServices:SimulationCore"] = "https://simulationcore.test",
            ["DatabaseStartup:Enabled"] = "false"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static T AssertResult<T>(IResult result, int expectedStatusCode)
    {
        IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(expectedStatusCode, status.StatusCode);

        IValueHttpResult value = Assert.IsAssignableFrom<IValueHttpResult>(result);
        return Assert.IsType<T>(value.Value);
    }

    public static void AssertStatus(IResult result, int expectedStatusCode)
    {
        IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(expectedStatusCode, status.StatusCode);
    }

    private static CityResourceSupplyConditionDto CreateResourceSupplyConditionDto()
    {
        return new CityResourceSupplyConditionDto(
            SupplyStressIndex: 0.28m,
            EffectiveAtUtc: LastEvaluatedAtUtc,
            Fuel: CreateResourceSupplyLineConditionDto(0.72m, 0.76m, 0.15m),
            SpareParts: CreateResourceSupplyLineConditionDto(0.68m, 0.73m, 0.18m),
            Filters: CreateResourceSupplyLineConditionDto(0.81m, 0.79m, 0.11m),
            EmergencyWater: CreateResourceSupplyLineConditionDto(0.75m, 0.70m, 0.16m));
    }

    private static CityResourceSupplyLineConditionDto CreateResourceSupplyLineConditionDto(
        decimal stockLevelIndex,
        decimal resupplyReadinessIndex,
        decimal shortageRiskIndex)
    {
        return new CityResourceSupplyLineConditionDto(
            StockLevelIndex: stockLevelIndex,
            ResupplyReadinessIndex: resupplyReadinessIndex,
            ShortageRiskIndex: shortageRiskIndex);
    }

    private static CitySystemConditionDto CreateSystemConditionDto(string kind)
    {
        return new CitySystemConditionDto(
            Kind: kind,
            LoadIndex: 0.34m,
            ServiceQualityIndex: 0.86m,
            BacklogIndex: 0.14m,
            FailureRiskIndex: 0.09m);
    }

    private static PendingCityOperationDto CreatePendingOperationDto(
        string focus = "Balanced",
        string intensity = "Elevated",
        long readyAtTickId = 42)
    {
        return new PendingCityOperationDto(
            Focus: focus,
            Intensity: intensity,
            ReadyAtTickId: readyAtTickId);
    }

    public sealed class FakeSender : IMediator
    {
        private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();

        public List<object> Requests { get; } = [];

        public void Handle<TRequest, TResponse>(Func<TRequest, TResponse> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = (request, _) => Task.FromResult<object?>(handler((TRequest)request));
        }

        public void Handle<TRequest>(Action<TRequest> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = (request, _) =>
            {
                handler((TRequest)request);
                return Task.FromResult<object?>(Unit.Value);
            };
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            return Invoke<TResponse>(handler, request, cancellationToken);
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            await handler(request, cancellationToken);
        }

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            return await handler(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        private static async Task<TResponse> Invoke<TResponse>(
            Func<object, CancellationToken, Task<object?>> handler,
            object request,
            CancellationToken cancellationToken)
        {
            object? result = await handler(request, cancellationToken);
            return (TResponse)result!;
        }
    }
}
