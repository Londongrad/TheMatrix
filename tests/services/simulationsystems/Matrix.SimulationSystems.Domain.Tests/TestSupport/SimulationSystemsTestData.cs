using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Domain.Tests.TestSupport;

internal static class SimulationSystemsTestData
{
    internal static readonly Guid CityId = Guid.Parse("73000000-0000-0000-0000-000000000001");
    internal static readonly DateTimeOffset CreatedAtUtc = new(2051, 2, 3, 8, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(6);

    internal static SimulationHostId CreateHostId()
    {
        return new SimulationHostId(CityId);
    }

    internal static CityEnvironmentalConditionPolicy CreatePolicy()
    {
        return new CityEnvironmentalConditionPolicy();
    }

    internal static CityEnvironmentalConditionSnapshot CreateSeed(
        string developmentLevel = "standard",
        DateTimeOffset? evaluatedAtUtc = null)
    {
        return CreatePolicy().CreateSeed(
            cityId: CityId,
            developmentLevel: developmentLevel,
            asOfUtc: evaluatedAtUtc ?? CreatedAtUtc);
    }

    internal static CityEnvironmentalConditionState CreateState(
        string developmentLevel = "standard",
        DateTimeOffset? evaluatedAtUtc = null)
    {
        return CityEnvironmentalConditionState.Create(
            simulationHostId: CreateHostId(),
            seed: CreateSeed(
                developmentLevel: developmentLevel,
                evaluatedAtUtc: evaluatedAtUtc));
    }

    internal static CityEnvironmentalConditionSnapshot CreateUpdatedSnapshot(
        CityEnvironmentalConditionSnapshot baseline,
        DateTimeOffset evaluatedAtUtc)
    {
        return new CityEnvironmentalConditionSnapshot(
            drainage: baseline.Drainage,
            drainageInfrastructure: baseline.DrainageInfrastructure,
            snowRemoval: baseline.SnowRemoval,
            snowRemovalInfrastructure: baseline.SnowRemovalInfrastructure,
            roadAccess: baseline.RoadAccess,
            roadAccessInfrastructure: baseline.RoadAccessInfrastructure,
            heating: baseline.Heating,
            heatingInfrastructure: baseline.HeatingInfrastructure,
            waterDistribution: baseline.WaterDistribution,
            waterDistributionInfrastructure: baseline.WaterDistributionInfrastructure,
            sanitation: baseline.Sanitation,
            sanitationInfrastructure: baseline.SanitationInfrastructure,
            floodingIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.FloodingIndex.From(0.42m),
            snowAccumulationIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.SnowAccumulationIndex.From(0.27m),
            roadAccessibilityIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.RoadAccessibilityIndex.From(0.68m),
            heatingCoverageIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.HeatingCoverageIndex.From(0.81m),
            waterCoverageIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.WaterCoverageIndex.From(0.77m),
            sanitationCoverageIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.SanitationCoverageIndex.From(0.74m),
            evaluatedAtUtc: evaluatedAtUtc,
            powerDistribution: baseline.PowerDistribution,
            powerDistributionInfrastructure: baseline.PowerDistributionInfrastructure,
            powerCoverageIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.PowerCoverageIndex.From(0.83m),
            utilityIncidents: baseline.UtilityIncidents,
            utilityIncidentInfrastructure: baseline.UtilityIncidentInfrastructure,
            utilityContinuityIndex: Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects.UtilityContinuityIndex.From(0.79m),
            resourceSupply: CityResourceSupplySnapshot.Neutral(
                effectiveAtUtc: evaluatedAtUtc,
                effectiveTickId: 4),
            operationalBudgetPressure: CityOperationalBudgetPressureSnapshot.Neutral(
                effectiveAtUtc: evaluatedAtUtc,
                effectiveTickId: 4));
    }
}
