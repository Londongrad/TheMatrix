using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Simulation;
using Matrix.SimulationSystems.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Tests.TestSupport
{
    internal static class SimulationSystemsInfrastructureTestSupport
    {
        internal static readonly Guid CityId = Guid.Parse("74000000-0000-0000-0000-000000000001");

        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2053,
            month: 4,
            day: 5,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(3);

        internal static SimulationSystemsDbContext CreateDbContext(string? databaseName = null)
        {
            DbContextOptions<SimulationSystemsDbContext> options =
                new DbContextOptionsBuilder<SimulationSystemsDbContext>()
                   .UseInMemoryDatabase(
                        databaseName ??
                        Guid.NewGuid()
                           .ToString("N"))
                   .Options;

            var dbContext = new SimulationSystemsDbContext(options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }

        internal static SimulationHostId CreateHostId()
        {
            return new SimulationHostId(CityId);
        }

        internal static CityEnvironmentalConditionState CreateState(
            string developmentLevel = "standard",
            long lastAppliedTickId = 4)
        {
            var policy = new CityEnvironmentalConditionPolicy();
            CityEnvironmentalConditionSnapshot seed = policy.CreateSeed(
                cityId: CityId,
                developmentLevel: developmentLevel,
                asOfUtc: CreatedAtUtc);

            var state = CityEnvironmentalConditionState.Create(
                simulationHostId: CreateHostId(),
                seed: seed);

            state.MarkTickApplied(lastAppliedTickId);
            return state;
        }

        internal static ClassicCityOperationalExpenseIncurredV1 CreateOperationalExpenseEvent()
        {
            return new ClassicCityOperationalExpenseIncurredV1(
                ExpenseId: Guid.Parse("74000000-0000-0000-0000-000000000101"),
                CityId: CityId,
                Category: "Infrastructure",
                Amount: 275m,
                Title: "Dispatch sanitation maintenance",
                Description: "Infrastructure maintenance dispatched.",
                SourceService: "SimulationSystems",
                OperationKind: "SanitationMaintenanceDispatch",
                OccurredAtUtc: LaterUtc);
        }

        internal static ClassicCityLivingConditionsSnapshotV1 CreateLivingConditionsSnapshotEvent()
        {
            return new ClassicCityLivingConditionsSnapshotV1(
                CityId: CityId,
                FloodingIndex: 0.18m,
                RoadAccessibilityIndex: 0.76m,
                PowerCoverageIndex: 0.81m,
                UtilityContinuityIndex: 0.74m,
                HeatingCoverageIndex: 0.79m,
                WaterCoverageIndex: 0.84m,
                SanitationCoverageIndex: 0.77m,
                EffectiveTickId: 7,
                EffectiveAtUtc: LaterUtc.AddMinutes(-10),
                OccurredAtUtc: LaterUtc);
        }

        internal static ClassicCitySystemsResourceDemandSnapshotV1 CreateSystemsResourceDemandSnapshotEvent()
        {
            return new ClassicCitySystemsResourceDemandSnapshotV1(
                CityId: CityId,
                FuelDemandPressureIndex: 0.51m,
                SparePartsDemandPressureIndex: 0.49m,
                FiltersDemandPressureIndex: 0.38m,
                EmergencyWaterDemandPressureIndex: 0.29m,
                OverallDemandPressureIndex: 0.42m,
                EffectiveTickId: 7,
                EffectiveAtUtc: LaterUtc.AddMinutes(-5),
                OccurredAtUtc: LaterUtc);
        }
    }
}
