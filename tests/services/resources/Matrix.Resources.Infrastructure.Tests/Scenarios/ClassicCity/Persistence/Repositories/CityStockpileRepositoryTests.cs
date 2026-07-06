using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Infrastructure.Persistence;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityStockpileRepositoryTests
    {
        [Fact]
        public async Task AddAsync_PersistsStateAndGetBySimulationHostIdReturnsIt()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);
            CityStockpileState state = CreateState();
            state.ApplyHealthcareMedicineDemand(
                new CityHealthcareMedicineDemandPolicy().CreateDemand(
                    processedPatientCount: 100,
                    routineCareDeliveryCount: 4,
                    urgentCareDeliveryCount: 3,
                    acuteCareDeliveryCount: 2,
                    emergencyCareDeliveryCount: 1,
                    sourceRevision: 17,
                    careDate: new DateOnly(2048, 5, 6),
                    observedAtUtc: CreatedAtUtc));

            await repository.AddAsync(
                state: state,
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            CityStockpileState? loaded = await repository.GetBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(
                expected: CreateHostId(),
                actual: loaded!.SimulationHostId);
            Assert.Equal(
                expected: state.LastAppliedTickId,
                actual: loaded.LastAppliedTickId);
            Assert.Equal(
                expected: state.SupplyStressIndex,
                actual: loaded.SupplyStressIndex);
            Assert.Equal(17, loaded.HealthcareMedicineDemand.SourceRevision);
            Assert.Equal(0.0500m, loaded.HealthcareMedicineDemand.MedicineLoadIndex);
            Assert.Equal(100, loaded.HealthcareMedicineDemand.ProcessedPatientCount);
        }

        [Fact]
        public async Task GetBySimulationHostIdAsync_ReturnsNullWhenStateDoesNotExist()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);

            CityStockpileState? loaded = await repository.GetBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.Null(loaded);
        }

        [Fact]
        public async Task DeleteBySimulationHostIdAsync_RemovesExistingStateAndIgnoresMissingState()
        {
            await using ResourcesDbContext dbContext = CreateDbContext();
            var repository = new CityStockpileRepository(dbContext);
            await repository.AddAsync(
                state: CreateState(),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();

            await repository.DeleteBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);
            await dbContext.SaveChangesAsync();
            await repository.DeleteBySimulationHostIdAsync(
                simulationHostId: CreateHostId(),
                cancellationToken: CancellationToken.None);

            Assert.Null(
                await repository.GetBySimulationHostIdAsync(
                    simulationHostId: CreateHostId(),
                    cancellationToken: CancellationToken.None));
        }
    }
}
