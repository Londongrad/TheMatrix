using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class CityRepositoryTests
    {
        [Fact]
        public async Task ListAsync_WhenArchivedCitiesAreExcluded_ReturnsOnlyActiveCitiesOrderedByCreatedAt()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListAsync_WhenArchivedCitiesAreExcluded_ReturnsOnlyActiveCitiesOrderedByCreatedAt));
            DateTimeOffset baseUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);

            City activeOlder = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc,
                name: "Active Older");
            City archivedCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(1),
                name: "Archived");
            archivedCity.Archive(baseUtc.AddHours(1));
            City activeNewer = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(2),
                name: "Active Newer");

            await dbContext.Cities.AddRangeAsync(
                activeOlder,
                archivedCity,
                activeNewer);
            await dbContext.SaveChangesAsync();
            var repository = new CityRepository(dbContext);

            IReadOnlyList<City> result = await repository.ListAsync(
                includeArchived: false,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    activeOlder.Id,
                    activeNewer.Id
                ],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
        }

        [Fact]
        public async Task ListProvisioningAsync_ReturnsProvisioningFailuresFirstThenNewestItems()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListProvisioningAsync_ReturnsProvisioningFailuresFirstThenNewestItems));
            DateTimeOffset baseUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);

            City failedCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc,
                requiresPopulationBootstrap: true,
                name: "Failed");
            failedCity.TryFailPopulationBootstrap(
                operationId: failedCity.PopulationBootstrapOperationId,
                failureCode: "timeout",
                failedAtUtc: baseUtc.AddMinutes(5));

            City provisioningOlder = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(1),
                requiresPopulationBootstrap: true,
                name: "Provisioning Older");
            City provisioningNewer = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(2),
                requiresPopulationBootstrap: true,
                name: "Provisioning Newer");
            City activeCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(3),
                name: "Active");

            await dbContext.Cities.AddRangeAsync(
                failedCity,
                provisioningOlder,
                provisioningNewer,
                activeCity);
            await dbContext.SaveChangesAsync();
            var repository = new CityRepository(dbContext);

            IReadOnlyList<City> result = await repository.ListProvisioningAsync(CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    failedCity.Id,
                    provisioningNewer.Id,
                    provisioningOlder.Id
                ],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
        }

        [Fact]
        public async Task ListRecoverableProvisioningAsync_ReturnsExpiredAndUnleasedCitiesInRecoveryOrder()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(ListRecoverableProvisioningAsync_ReturnsExpiredAndUnleasedCitiesInRecoveryOrder));
            DateTimeOffset baseUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
            DateTimeOffset asOfUtc = baseUtc.AddMinutes(30);

            City expiredLeaseCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc,
                requiresPopulationBootstrap: true,
                name: "Expired Lease");
            expiredLeaseCity.TryAcquireProvisioningLease(
                acquiredAtUtc: baseUtc.AddMinutes(1),
                leaseDuration: TimeSpan.FromMinutes(5));

            City noLeaseCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(2),
                requiresPopulationBootstrap: true,
                name: "No Lease");

            City activeLeaseCity = SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: baseUtc.AddMinutes(3),
                requiresPopulationBootstrap: true,
                name: "Active Lease");
            activeLeaseCity.TryAcquireProvisioningLease(
                acquiredAtUtc: asOfUtc.AddMinutes(-1),
                leaseDuration: TimeSpan.FromMinutes(10));

            await dbContext.Cities.AddRangeAsync(
                expiredLeaseCity,
                noLeaseCity,
                activeLeaseCity);
            await dbContext.SaveChangesAsync();
            var repository = new CityRepository(dbContext);

            IReadOnlyList<City> result = await repository.ListRecoverableProvisioningAsync(
                asOfUtc: asOfUtc,
                limit: 5,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    noLeaseCity.Id,
                    expiredLeaseCity.Id
                ],
                actualArray: result.Select(static x => x.Id)
                   .ToArray());
        }

        [Fact]
        public async Task GetByProvisioningCorrelationIdAsync_WhenCityExists_ReturnsMatchingCity()
        {
            using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
                nameof(GetByProvisioningCorrelationIdAsync_WhenCityExists_ReturnsMatchingCity));
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 2,
                day: 3,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero);
            var provisioningCorrelationId = Guid.NewGuid();
            var city = City.Create(
                name: new CityName("Provisioned"),
                simulationKind: SimulationKind.ClassicCity,
                environment: CityEnvironment.Create(
                    climateZone: ClimateZone.Temperate,
                    hemisphere: Hemisphere.Northern,
                    utcOffset: CityUtcOffset.FromMinutes(180)),
                generationSeed: new CityGenerationSeed("correlation-seed"),
                scenarioModelSetVersion: new ScenarioModelSetVersion("classic-city-v3"),
                generationProfile: CityGenerationProfile.Create(
                    sizeTier: CitySizeTier.Medium,
                    urbanDensity: UrbanDensity.Balanced,
                    developmentLevel: CityDevelopmentLevel.Balanced,
                    economyProfile: CityEconomyProfile.Balanced,
                    populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                    plannedPeopleCount: 15_000),
                initialWeatherProfile: CityInitialWeatherProfile.CreateRandom(),
                provisioningCorrelationId: provisioningCorrelationId,
                requiresPopulationBootstrap: false,
                requiresEconomyBootstrap: false,
                createdAtUtc: createdAtUtc);
            await dbContext.Cities.AddAsync(city);
            await dbContext.SaveChangesAsync();
            var repository = new CityRepository(dbContext);

            City? result = await repository.GetByProvisioningCorrelationIdAsync(
                provisioningCorrelationId: provisioningCorrelationId,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: city.Id,
                actual: result.Id);
            Assert.Equal(
                expected: provisioningCorrelationId,
                actual: result.ProvisioningCorrelationId);
        }
    }
}
