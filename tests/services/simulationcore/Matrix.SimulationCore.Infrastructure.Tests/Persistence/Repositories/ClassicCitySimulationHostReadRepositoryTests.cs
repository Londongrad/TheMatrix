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
        public async Task GetBySimulationIdAsync_WhenCityIsMissing_ReturnsNull()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(GetBySimulationIdAsync_WhenCityIsMissing_ReturnsNull));
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
        public async Task GetBySimulationIdAsync_MapsCityStatusToSimulationHostState(
            CityStatus cityStatus,
            SimulationHostState expectedState)
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                $"{nameof(GetBySimulationIdAsync_MapsCityStatusToSimulationHostState)}_{cityStatus}");
            City city = CreateCityWithStatus(cityStatus);
            await dbContext.Cities.AddAsync(city);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            var repository = new ClassicCitySimulationHostReadRepository(dbContext);

            SimulationHost? result = await repository.GetBySimulationIdAsync(
                simulationId: new SimulationId(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: new SimulationId(city.Id.Value),
                actual: result.SimulationId);
            Assert.Equal(
                expected: new SimulationHostId(city.Id.Value),
                actual: result.HostId);
            Assert.Equal(
                expected: SimulationHostKind.City,
                actual: result.HostKind);
            Assert.Equal(
                expected: SimulationKind.ClassicCity,
                actual: result.SimulationKind);
            Assert.Equal(
                expected: expectedState,
                actual: result.State);
            Assert.Equal(
                expected: city.CreatedAtUtc,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: city.ArchivedAtUtc,
                actual: result.ArchivedAtUtc);
            Assert.Empty(dbContext.ChangeTracker.Entries<City>());
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
