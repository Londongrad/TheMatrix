using Matrix.ApiGateway.Contracts.SimulationCore.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Dashboard;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Dashboard;

public sealed class CityOperationsDashboardAlertBuilderTests
{
    [Fact]
    public void Build_WhenSnapshotsHaveNoPanelData_ReturnsEmptyAlertCollections()
    {
        var builder = new CityOperationsDashboardAlertBuilder();

        CityOperationsDashboardAlerts alerts = builder.Build([CreateSnapshot()]);

        Assert.Empty(alerts.EnvironmentalAlerts);
        Assert.Empty(alerts.PopulationDistrictAlerts);
        Assert.Empty(alerts.DistrictResponsePriorities);
        Assert.Empty(alerts.MobilityAlerts);
        Assert.Empty(alerts.BudgetAlerts);
        Assert.Empty(alerts.TickFreshnessAlerts);
        Assert.Empty(alerts.PhaseProgressAlerts);
    }

    [Fact]
    public void Build_WhenBudgetPressureIsHigh_ReturnsBudgetAlert()
    {
        CityListItemView city = CreateCity(name: "Budget City");
        CityOperationalBudgetPressureView pressure = CreateBudgetPressure(
            cityId: city.CityId,
            balance: -100m,
            pressureIndex: 0.7200m);
        var builder = new CityOperationsDashboardAlertBuilder();

        CityOperationsDashboardAlerts alerts = builder.Build(
            [CreateSnapshot(
                city: city,
                budget: pressure)]);

        DashboardBudgetPressureView alert = Assert.Single(alerts.BudgetAlerts);
        Assert.Equal(city.CityId, alert.CityId);
        Assert.Equal(city.Name, alert.CityName);
        Assert.Equal("danger", alert.Severity);
        Assert.Equal(pressure.PressureIndex, alert.PressureIndex);
        Assert.Same(pressure, alert.Budget);
        Assert.False(string.IsNullOrWhiteSpace(alert.Summary));
        Assert.Empty(alerts.EnvironmentalAlerts);
        Assert.Empty(alerts.PopulationDistrictAlerts);
        Assert.Empty(alerts.DistrictResponsePriorities);
        Assert.Empty(alerts.MobilityAlerts);
        Assert.Empty(alerts.TickFreshnessAlerts);
        Assert.Empty(alerts.PhaseProgressAlerts);
    }

    [Fact]
    public void Build_WhenMultipleBudgetAlertsExist_SortsDangerBeforeWarning()
    {
        CityListItemView dangerCity = CreateCity(name: "Alpha");
        CityListItemView warningCity = CreateCity(name: "Bravo");
        var builder = new CityOperationsDashboardAlertBuilder();

        CityOperationsDashboardAlerts alerts = builder.Build(
            [
                CreateSnapshot(
                    city: warningCity,
                    budget: CreateBudgetPressure(
                        cityId: warningCity.CityId,
                        pressureIndex: 0.4500m)),
                CreateSnapshot(
                    city: dangerCity,
                    budget: CreateBudgetPressure(
                        cityId: dangerCity.CityId,
                        pressureIndex: 0.7200m))
            ]);

        Assert.Collection(
            alerts.BudgetAlerts,
            first =>
            {
                Assert.Equal(dangerCity.CityId, first.CityId);
                Assert.Equal("danger", first.Severity);
            },
            second =>
            {
                Assert.Equal(warningCity.CityId, second.CityId);
                Assert.Equal("warning", second.Severity);
            });
    }

    [Fact]
    public void Build_WhenSystemsAndBudgetTicksAreSkewed_ReturnsTickFreshnessAlert()
    {
        CityListItemView city = CreateCity(name: "Skew City");
        CityEnvironmentalConditionsView conditions = CreateEnvironmentalConditions(
            cityId: city.CityId,
            effectiveTickId: 10);
        CityOperationalBudgetPressureView budget = CreateBudgetPressure(
            cityId: city.CityId,
            effectiveTickId: 14,
            pressureIndex: 0.1500m);
        var builder = new CityOperationsDashboardAlertBuilder();

        CityOperationsDashboardAlerts alerts = builder.Build(
            [CreateSnapshot(
                city: city,
                conditions: conditions,
                budget: budget)]);

        DashboardTickFreshnessView alert = Assert.Single(alerts.TickFreshnessAlerts);
        Assert.Equal(city.CityId, alert.CityId);
        Assert.Equal(10, alert.EnvironmentalTickId);
        Assert.Equal(14, alert.BudgetTickId);
        Assert.Equal(4, alert.TickSkew);
        Assert.Equal("warning", alert.Severity);
        Assert.False(string.IsNullOrWhiteSpace(alert.Summary));
        Assert.Empty(alerts.EnvironmentalAlerts);
        Assert.Empty(alerts.PopulationDistrictAlerts);
        Assert.Empty(alerts.DistrictResponsePriorities);
        Assert.Empty(alerts.MobilityAlerts);
        Assert.Empty(alerts.BudgetAlerts);
        Assert.Empty(alerts.PhaseProgressAlerts);
    }

    private static CityOperationalSnapshot CreateSnapshot(
        CityListItemView? city = null,
        CityEnvironmentalConditionsView? conditions = null,
        CityOperationalBudgetPressureView? budget = null)
    {
        return new CityOperationalSnapshot(
            City: city ?? CreateCity(),
            Conditions: conditions,
            PopulationDistrictPressure: null,
            DistrictHeating: null,
            DistrictWater: null,
            DistrictPower: null,
            DistrictSanitation: null,
            DistrictUtilityIncidents: null,
            ActiveTrips: null,
            Stockpiles: null,
            Budget: budget);
    }

    private static CityListItemView CreateCity(string name = "Neo City")
    {
        DateTimeOffset createdAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new CityListItemView(
            CityId: Guid.NewGuid(),
            SimulationId: Guid.NewGuid(),
            Name: name,
            SimulationKind: "ClassicCity",
            Status: "Active",
            CreatedAtUtc: createdAtUtc,
            PopulationBootstrapCompletedAtUtc: createdAtUtc.AddMinutes(5),
            PopulationBootstrapFailedAtUtc: null,
            PopulationBootstrapFailureCode: null,
            ArchivedAtUtc: null);
    }

    private static CityEnvironmentalConditionsView CreateEnvironmentalConditions(
        Guid cityId,
        long effectiveTickId)
    {
        DateTimeOffset evaluatedAtUtc = new(2048, 6, 3, 13, 5, 0, TimeSpan.Zero);
        var line = new CityResourceSupplyLineConditionView(
            StockLevelIndex: 0.95m,
            ResupplyReadinessIndex: 0.95m,
            ShortageRiskIndex: 0.02m);
        var system = new CitySystemConditionView(
            Kind: "Stable",
            LoadIndex: 0.08m,
            ServiceQualityIndex: 0.95m,
            BacklogIndex: 0.02m,
            FailureRiskIndex: 0.02m);

        return new CityEnvironmentalConditionsView(
            CityId: cityId,
            EffectiveTickId: effectiveTickId,
            EffectivePhase: "SystemsSettled",
            FloodingIndex: 0.02m,
            SnowAccumulationIndex: 0.01m,
            RoadAccessibilityIndex: 0.98m,
            PowerCoverageIndex: 0.98m,
            UtilityContinuityIndex: 0.98m,
            HeatingCoverageIndex: 0.98m,
            WaterCoverageIndex: 0.98m,
            SanitationCoverageIndex: 0.98m,
            LastEvaluatedAtUtc: evaluatedAtUtc,
            ResourceSupply: new CityResourceSupplyConditionView(
                SupplyStressIndex: 0.02m,
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

    private static CityOperationalBudgetPressureView CreateBudgetPressure(
        Guid cityId,
        long effectiveTickId = 17,
        decimal balance = 100000m,
        decimal pressureIndex = 0.1500m)
    {
        return new CityOperationalBudgetPressureView(
            CityId: cityId,
            EffectiveTickId: effectiveTickId,
            EffectivePhase: "BudgetSettled",
            EffectiveAtUtc: new DateTimeOffset(2048, 6, 3, 13, 7, 0, TimeSpan.Zero),
            UnitKind: "Currency",
            UnitCode: "CR",
            UnitDisplayName: "Credits",
            UnitSymbol: "C",
            Balance: balance,
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
            PressureIndex: pressureIndex);
    }
}
