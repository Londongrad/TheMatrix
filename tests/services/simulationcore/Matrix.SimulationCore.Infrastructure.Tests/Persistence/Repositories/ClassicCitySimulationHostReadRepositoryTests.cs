using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class ClassicCitySimulationHostReadRepositoryTests
    {
        [Fact]
        public async Task GetBySimulationIdAsync_WhenInstanceIsMissing_ReturnsNull()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(GetBySimulationIdAsync_WhenInstanceIsMissing_ReturnsNull));
            var repository = new ClassicCitySimulationHostReadRepository(dbContext);

            SimulationHost? result = await repository.GetBySimulationIdAsync(
                simulationId: new SimulationId(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(
            CityStatus.Active,
            SimulationHostState.Active)]
        [InlineData(
            CityStatus.Provisioning,
            SimulationHostState.Provisioning)]
        [InlineData(
            CityStatus.ProvisioningFailed,
            SimulationHostState.ProvisioningFailed)]
        [InlineData(
            CityStatus.Archived,
            SimulationHostState.Archived)]
        public async Task GetBySimulationIdAsync_ProjectsRuntimeInstanceState(
            CityStatus cityStatus,
            SimulationHostState expectedState)
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                $"{nameof(GetBySimulationIdAsync_ProjectsRuntimeInstanceState)}_{cityStatus}");
            City city = CreateCityWithStatus(cityStatus);
            SimulationInstance instance = SimulationInfrastructureTestSupport.CreateInstance(city);
            await dbContext.SimulationInstances.AddAsync(instance);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new ClassicCitySimulationHostReadRepository(dbContext);

            SimulationHost? result = await repository.GetBySimulationIdAsync(
                simulationId: new SimulationId(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: instance.Id,
                actual: result.SimulationId);
            Assert.Equal(
                expected: instance.HostId,
                actual: result.HostId);
            Assert.Equal(
                expected: instance.RuntimeKey,
                actual: result.RuntimeKey);
            Assert.Equal(
                expected: expectedState,
                actual: result.State);
            Assert.Equal(
                expected: instance.CreatedAtUtc,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: instance.ArchivedAtUtc,
                actual: result.ArchivedAtUtc);
            Assert.Empty(dbContext.ChangeTracker.Entries<SimulationInstance>());
        }

        private static City CreateCityWithStatus(CityStatus status)
        {
            City city = status switch
            {
                CityStatus.Provisioning => SimulationInfrastructureTestSupport.CreateCity(
                    createdAtUtc: RepositoryTestData.BaseUtc,
                    requiresPopulationBootstrap: true,
                    name: "Provisioning City"),
                CityStatus.ProvisioningFailed => SimulationInfrastructureTestSupport.CreateCity(
                    createdAtUtc: RepositoryTestData.BaseUtc,
                    requiresPopulationBootstrap: true,
                    name: "Failed City"),
                _ => RepositoryTestData.CreateCity(name: $"{status} City")
            };

            if (status == CityStatus.ProvisioningFailed)
                city.TryFailPopulationBootstrap(
                    operationId: city.PopulationBootstrapOperationId,
                    failureCode: "timeout",
                    failedAtUtc: RepositoryTestData.BaseUtc.AddMinutes(5));

            if (status == CityStatus.Archived)
                city.Archive(RepositoryTestData.BaseUtc.AddHours(1));

            return city;
        }
    }
}
