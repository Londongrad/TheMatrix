using System.Net;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard;

public sealed class CityOperationsDashboardSnapshotLoaderTests
{
    [Fact]
    public async Task LoadReadyClassicCitySnapshotsAsync_WhenReadyClassicCityExists_LoadsAllPanelData()
    {
        CityListItemView city = CreateCity();
        var economyClient = new RecordingEconomyApiClient
        {
            BudgetPressureResult = CreateBudgetPressure(city.CityId)
        };
        var populationClient = new RecordingPopulationApiClient
        {
            DistrictPressureResult = CreateCityPopulationDistrictPressureDto(city.CityId)
        };
        var stockpilesClient = new RecordingStockpilesApiClient
        {
            StockpilesResult = CreateStockpiles(city.CityId)
        };
        var tripsClient = new RecordingTripsApiClient
        {
            Result = [CreateActiveTrip(city.CityId)]
        };
        var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
        {
            ConditionsResult = CreateEnvironmentalConditions(city.CityId)
        };
        CityOperationsDashboardSnapshotLoader loader = CreateLoader(
            economyClient: economyClient,
            populationClient: populationClient,
            stockpilesClient: stockpilesClient,
            tripsClient: tripsClient,
            environmentalConditionsClient: environmentalConditionsClient);

        IReadOnlyList<CityOperationalSnapshot> snapshots =
            await loader.LoadReadyClassicCitySnapshotsAsync([city], CancellationToken.None);

        CityOperationalSnapshot snapshot = Assert.Single(snapshots);
        Assert.Same(city, snapshot.City);
        Assert.NotNull(snapshot.Conditions);
        Assert.NotNull(snapshot.PopulationDistrictPressure);
        Assert.NotNull(snapshot.DistrictHeating);
        Assert.NotNull(snapshot.DistrictWater);
        Assert.NotNull(snapshot.DistrictPower);
        Assert.NotNull(snapshot.DistrictSanitation);
        Assert.NotNull(snapshot.DistrictUtilityIncidents);
        Assert.NotNull(snapshot.ActiveTrips);
        Assert.NotNull(snapshot.Stockpiles);
        Assert.NotNull(snapshot.Budget);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastConditionsCityId);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastHeatingCityId);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastWaterCityId);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastPowerCityId);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastSanitationCityId);
        Assert.Equal(city.CityId, environmentalConditionsClient.LastUtilityIncidentCityId);
        Assert.Equal(city.CityId, populationClient.LastDistrictPressureCityId);
        Assert.Equal(city.CityId, tripsClient.LastCityId);
        Assert.Equal(city.CityId, stockpilesClient.LastStockpilesCityId);
        Assert.Equal(city.CityId, economyClient.LastBudgetPressureCityId);
    }

    [Fact]
    public async Task LoadReadyClassicCitySnapshotsAsync_WhenCitiesAreNotReadyClassicCity_SkipsPanelLoads()
    {
        CityListItemView readyClassicCity = CreateCity(name: "Ready Classic");
        CityListItemView otherSimulationKind = CreateCity(
            name: "Different Kind",
            simulationKind: "OtherSimulation");
        CityListItemView archivedClassicCity = CreateCity(
            name: "Archived Classic",
            archivedAtUtc: new DateTimeOffset(2048, 6, 2, 0, 0, 0, TimeSpan.Zero));
        CityListItemView provisioningClassicCity = CreateCity(
            name: "Provisioning Classic",
            status: "Provisioning");
        var economyClient = new RecordingEconomyApiClient();
        var populationClient = new RecordingPopulationApiClient();
        var stockpilesClient = new RecordingStockpilesApiClient();
        var tripsClient = new RecordingTripsApiClient();
        var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient();
        CityOperationsDashboardSnapshotLoader loader = CreateLoader(
            economyClient: economyClient,
            populationClient: populationClient,
            stockpilesClient: stockpilesClient,
            tripsClient: tripsClient,
            environmentalConditionsClient: environmentalConditionsClient);

        IReadOnlyList<CityOperationalSnapshot> snapshots =
            await loader.LoadReadyClassicCitySnapshotsAsync(
                [readyClassicCity, otherSimulationKind, archivedClassicCity, provisioningClassicCity],
                CancellationToken.None);

        CityOperationalSnapshot snapshot = Assert.Single(snapshots);
        Assert.Same(readyClassicCity, snapshot.City);
        Assert.Equal(1, environmentalConditionsClient.ConditionsCallCount);
        Assert.Equal(1, environmentalConditionsClient.HeatingCallCount);
        Assert.Equal(1, environmentalConditionsClient.WaterCallCount);
        Assert.Equal(1, environmentalConditionsClient.PowerCallCount);
        Assert.Equal(1, environmentalConditionsClient.SanitationCallCount);
        Assert.Equal(1, environmentalConditionsClient.UtilityIncidentCallCount);
        Assert.Equal(1, populationClient.DistrictPressureCallCount);
        Assert.Equal(1, tripsClient.CallCount);
        Assert.Equal(1, stockpilesClient.StockpilesCallCount);
        Assert.Equal(1, economyClient.BudgetPressureCallCount);
        Assert.Equal(readyClassicCity.CityId, populationClient.LastDistrictPressureCityId);
        Assert.Equal(readyClassicCity.CityId, tripsClient.LastCityId);
        Assert.Equal(readyClassicCity.CityId, stockpilesClient.LastStockpilesCityId);
        Assert.Equal(readyClassicCity.CityId, economyClient.LastBudgetPressureCityId);
    }

    [Fact]
    public async Task LoadReadyClassicCitySnapshotsAsync_WhenPanelLoadFails_ReturnsSnapshotWithNullPanelAndContinues()
    {
        CityListItemView city = CreateCity();
        var populationClient = new RecordingPopulationApiClient
        {
            DistrictPressureException = new HttpRequestException("population unavailable")
        };
        var economyClient = new RecordingEconomyApiClient
        {
            BudgetPressureResult = CreateBudgetPressure(city.CityId)
        };
        CityOperationsDashboardSnapshotLoader loader = CreateLoader(
            economyClient: economyClient,
            populationClient: populationClient);

        IReadOnlyList<CityOperationalSnapshot> snapshots =
            await loader.LoadReadyClassicCitySnapshotsAsync([city], CancellationToken.None);

        CityOperationalSnapshot snapshot = Assert.Single(snapshots);
        Assert.Same(city, snapshot.City);
        Assert.Null(snapshot.PopulationDistrictPressure);
        Assert.NotNull(snapshot.Budget);
        Assert.Equal(1, populationClient.DistrictPressureCallCount);
        Assert.Equal(1, economyClient.BudgetPressureCallCount);
    }

    private static CityOperationsDashboardSnapshotLoader CreateLoader(
        RecordingEconomyApiClient? economyClient = null,
        RecordingPopulationApiClient? populationClient = null,
        RecordingStockpilesApiClient? stockpilesClient = null,
        RecordingTripsApiClient? tripsClient = null,
        RecordingEnvironmentalConditionsApiClient? environmentalConditionsClient = null,
        int panelReadTimeoutSeconds = 5,
        int maxConcurrentCitySnapshotLoads = 2)
    {
        return new CityOperationsDashboardSnapshotLoader(
            economyClient: economyClient ?? new RecordingEconomyApiClient(),
            populationClient: populationClient ?? new RecordingPopulationApiClient(),
            stockpilesClient: stockpilesClient ?? new RecordingStockpilesApiClient(),
            tripsClient: tripsClient ?? new RecordingTripsApiClient(),
            environmentalConditionsClient: environmentalConditionsClient ?? new RecordingEnvironmentalConditionsApiClient(),
            dashboardOptions: Options.Create(new CityOperationsDashboardOptions
            {
                PanelReadTimeoutSeconds = panelReadTimeoutSeconds,
                HealthProbeTimeoutSeconds = 5,
                MaxConcurrentCitySnapshotLoads = maxConcurrentCitySnapshotLoads
            }),
            logger: NullLogger<CityOperationsDashboardSnapshotLoader>.Instance);
    }

    private static CityListItemView CreateCity(
        string name = "Neo City",
        string simulationKind = "ClassicCity",
        string status = "Active",
        DateTimeOffset? archivedAtUtc = null)
    {
        DateTimeOffset createdAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new CityListItemView(
            CityId: Guid.NewGuid(),
            SimulationId: Guid.NewGuid(),
            Name: name,
            SimulationKind: simulationKind,
            Status: status,
            CreatedAtUtc: createdAtUtc,
            PopulationBootstrapCompletedAtUtc: status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                ? createdAtUtc.AddMinutes(10)
                : null,
            PopulationBootstrapFailedAtUtc: null,
            PopulationBootstrapFailureCode: null,
            ArchivedAtUtc: archivedAtUtc);
    }

    private static CityEnvironmentalConditionsView CreateEnvironmentalConditions(Guid cityId)
    {
        DateTimeOffset evaluatedAtUtc = new(2048, 6, 3, 13, 5, 0, TimeSpan.Zero);
        var line = new CityResourceSupplyLineConditionView(
            StockLevelIndex: 0.82m,
            ResupplyReadinessIndex: 0.74m,
            ShortageRiskIndex: 0.18m);
        var system = new CitySystemConditionView(
            Kind: "Drainage",
            LoadIndex: 0.22m,
            ServiceQualityIndex: 0.86m,
            BacklogIndex: 0.12m,
            FailureRiskIndex: 0.08m);

        return new CityEnvironmentalConditionsView(
            CityId: cityId,
            EffectiveTickId: 17,
            EffectivePhase: "SystemsSettled",
            FloodingIndex: 0.12m,
            SnowAccumulationIndex: 0.03m,
            RoadAccessibilityIndex: 0.91m,
            PowerCoverageIndex: 0.94m,
            UtilityContinuityIndex: 0.89m,
            HeatingCoverageIndex: 0.92m,
            WaterCoverageIndex: 0.9m,
            SanitationCoverageIndex: 0.88m,
            LastEvaluatedAtUtc: evaluatedAtUtc,
            ResourceSupply: new CityResourceSupplyConditionView(
                SupplyStressIndex: 0.2m,
                EffectiveAtUtc: evaluatedAtUtc,
                Fuel: line,
                SpareParts: line,
                Filters: line,
                EmergencyWater: line),
            Drainage: system,
            SnowRemoval: system,
            RoadAccess: system,
            PowerDistribution: system,
            UtilityIncidents: system,
            Heating: system,
            WaterDistribution: system,
            Sanitation: system);
    }

    private static CityStockpilesView CreateStockpiles(Guid cityId)
    {
        var line = new CityStockpileLineView(
            Kind: "Fuel",
            StockLevelIndex: 0.8m,
            DemandPressureIndex: 0.2m,
            ResupplyReadinessIndex: 0.7m,
            ShortageRiskIndex: 0.1m);

        return new CityStockpilesView(
            CityId: cityId,
            EffectiveTickId: 17,
            EffectivePhase: "ResourcesSettled",
            SupplyStressIndex: 0.2m,
            EmergencyRationingEnabled: false,
            LastEvaluatedAtUtc: new DateTimeOffset(2048, 6, 3, 13, 6, 0, TimeSpan.Zero),
            PendingResupply: null,
            Fuel: line,
            Food: line,
            Medicine: line,
            SpareParts: line,
            Filters: line,
            EmergencyWater: line);
    }

    private static CityOperationalBudgetPressureView CreateBudgetPressure(Guid cityId)
    {
        return new CityOperationalBudgetPressureView(
            CityId: cityId,
            EffectiveTickId: 17,
            EffectivePhase: "BudgetSettled",
            EffectiveAtUtc: new DateTimeOffset(2048, 6, 3, 13, 7, 0, TimeSpan.Zero),
            UnitKind: "Currency",
            UnitCode: "CR",
            UnitDisplayName: "Credits",
            UnitSymbol: "C",
            Balance: 100000m,
            TotalCityExpenses: 2000m,
            MunicipalOperationsExpenses: 1500m,
            InfrastructureOperationsExpenses: 600m,
            EmergencyOperationsExpenses: 300m,
            GeneralAvailableAmount: 50000m,
            OperationsAvailableAmount: 30000m,
            InfrastructureAvailableAmount: 20000m,
            HealthcareAvailableAmount: 10000m,
            GeneralAuthorizationLevel: "High",
            OperationsAuthorizationLevel: "High",
            InfrastructureAuthorizationLevel: "High",
            HealthcareAuthorizationLevel: "High",
            LastMunicipalExpenseAtUtc: "2048-06-03T13:00:00Z",
            PressureIndex: 0.15m);
    }

    private static CityActiveTripView CreateActiveTrip(Guid cityId)
    {
        Guid districtId = Guid.Parse("d5f64075-74ec-4fd5-a932-3fb6cf9f70c5");
        var endpoint = new CityActiveTripEndpointView(
            Kind: "District",
            EntityId: districtId,
            DistrictId: districtId,
            RoadNodeId: Guid.Parse("e29575db-d298-49c8-8b9d-e68355c94255"),
            Name: "Central",
            PositionX: 10m,
            PositionY: 20m);
        var progress = new CityActiveTripProgressView(
            DistrictId: districtId,
            RoadSegmentId: null,
            SegmentProgressIndex: 0.5m,
            PositionX: 15m,
            PositionY: 25m);

        return new CityActiveTripView(
            TripId: Guid.NewGuid(),
            CityId: cityId,
            TravellerEntityId: Guid.NewGuid(),
            Subject: "Resident",
            Purpose: "WorkCommute",
            Profile: "Default",
            Status: "Active",
            MovementCapabilityIndex: 0.9m,
            UsedDynamicRoadConditions: true,
            PlannedAtTickId: 10,
            ConditionsEffectiveTickId: 17,
            LastAdvancedTickId: 18,
            StartedAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 12, 0, 0, TimeSpan.Zero),
            LastAdvancedAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 12, 30, 0, TimeSpan.Zero),
            ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 13, 0, 0, TimeSpan.Zero),
            ArrivedAtSimTimeUtc: null,
            CurrentProgressIndex: 0.5m,
            TotalDistanceMeters: 1000m,
            DistanceTravelledMeters: 500m,
            RemainingDistanceMeters: 500m,
            PlannedTravelTimeMinutes: 45m,
            AdjustedTravelTimeMinutes: 50m,
            From: endpoint,
            To: endpoint,
            Current: progress);
    }
}
