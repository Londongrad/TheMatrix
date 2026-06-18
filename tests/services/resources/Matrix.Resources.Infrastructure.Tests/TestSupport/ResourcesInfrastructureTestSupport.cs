using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using Matrix.Resources.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Tests.TestSupport
{
    internal static class ResourcesInfrastructureTestSupport
    {
        internal static readonly Guid CityId = Guid.Parse("70000000-0000-0000-0000-000000000001");

        internal static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2050,
            month: 1,
            day: 1,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        internal static readonly DateTimeOffset LaterUtc = CreatedAtUtc.AddHours(2);

        internal static ResourcesDbContext CreateDbContext()
        {
            DbContextOptions<ResourcesDbContext> options = new DbContextOptionsBuilder<ResourcesDbContext>()
               .UseInMemoryDatabase(
                    Guid.NewGuid()
                       .ToString("N"))
               .Options;

            return new ResourcesDbContext(options);
        }

        internal static SimulationHostId CreateHostId()
        {
            return new SimulationHostId(CityId);
        }

        internal static CityStockpileState CreateState()
        {
            var policy = new CityStockpilePolicy();
            var state = CityStockpileState.Create(
                simulationHostId: CreateHostId(),
                seed: policy.CreateSeed(
                    developmentLevel: "advanced",
                    createdAtUtc: CreatedAtUtc));

            state.MarkTickApplied(4);
            return state;
        }

        internal static ClassicCityStockpileSnapshotV1 CreateStockpileSnapshotEvent()
        {
            CityStockpileState state = CreateState();

            return new ClassicCityStockpileSnapshotV1(
                CityId: CityId,
                SupplyStressIndex: state.SupplyStressIndex,
                EmergencyRationingEnabled: state.EmergencyRationingEnabled,
                Fuel: CreateLine(state.Fuel),
                Food: CreateLine(state.Food),
                Medicine: CreateLine(state.Medicine),
                SpareParts: CreateLine(state.SpareParts),
                Filters: CreateLine(state.Filters),
                EmergencyWater: CreateLine(state.EmergencyWater),
                EffectiveTickId: state.LastAppliedTickId,
                EffectiveAtUtc: state.LastEvaluatedAtUtc,
                OccurredAtUtc: LaterUtc);
        }

        internal static ClassicCityOperationalExpenseIncurredV1 CreateOperationalExpenseEvent()
        {
            return new ClassicCityOperationalExpenseIncurredV1(
                ExpenseId: Guid.Parse("70000000-0000-0000-0000-000000000101"),
                CityId: CityId,
                Category: "Operations",
                Amount: 240m,
                Title: "Dispatch citywide stockpile resupply",
                Description: "Operations stockpile resupply dispatched.",
                SourceService: "Resources",
                OperationKind: "StockpileResupplyDispatch",
                OccurredAtUtc: LaterUtc);
        }

        private static ClassicCityStockpileLineSnapshotV1 CreateLine(CityResourceStockLineState line)
        {
            return new ClassicCityStockpileLineSnapshotV1(
                Kind: line.Kind.ToString(),
                StockLevelIndex: line.StockLevelIndex,
                DemandPressureIndex: line.DemandPressureIndex,
                ResupplyReadinessIndex: line.ResupplyReadinessIndex,
                ShortageRiskIndex: line.ShortageRiskIndex);
        }
    }
}
