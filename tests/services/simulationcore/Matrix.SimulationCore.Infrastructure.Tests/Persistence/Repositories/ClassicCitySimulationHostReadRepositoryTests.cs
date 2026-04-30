using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories;

public sealed class ClassicCitySimulationHostReadRepositoryTests
{
    [Fact]
    public async Task GetBySimulationIdAsync_WhenCityIsMissing_ReturnsNull()
    {
        using SimulationCoreDbContext dbContext = SimulationInfrastructureTestSupport.CreateDbContext(
            nameof(GetBySimulationIdAsync_WhenCityIsMissing_ReturnsNull));
        var repository = new ClassicCitySimulationHostReadRepository(dbContext);

        SimulationHost? result = await repository.GetBySimulationIdAsync(
            new SimulationId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(CityStatus.Active, SimulationHostState.Active)]
    [InlineData(CityStatus.Provisioning, SimulationHostState.Provisioning)]
    [InlineData(CityStatus.ProvisioningFailed, SimulationHostState.ProvisioningFailed)]
    [InlineData(CityStatus.Archived, SimulationHostState.Archived)]
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
            new SimulationId(city.Id.Value),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new SimulationId(city.Id.Value), result.SimulationId);
        Assert.Equal(new SimulationHostId(city.Id.Value), result.HostId);
        Assert.Equal(SimulationHostKind.City, result.HostKind);
        Assert.Equal(SimulationKind.ClassicCity, result.SimulationKind);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(city.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(city.ArchivedAtUtc, result.ArchivedAtUtc);
        Assert.Empty(dbContext.ChangeTracker.Entries<City>());
    }

    private static City CreateCityWithStatus(CityStatus status)
    {
        City city = status switch
        {
            CityStatus.Provisioning => SimulationInfrastructureTestSupport.CreateCity(
                RepositoryTestData.BaseUtc,
                requiresPopulationBootstrap: true,
                name: "Provisioning City"),
            CityStatus.ProvisioningFailed => SimulationInfrastructureTestSupport.CreateCity(
                RepositoryTestData.BaseUtc,
                requiresPopulationBootstrap: true,
                name: "Failed City"),
            _ => RepositoryTestData.CreateCity(name: $"{status} City")
        };

        if (status == CityStatus.ProvisioningFailed)
        {
            city.TryFailPopulationBootstrap(
                city.PopulationBootstrapOperationId,
                "timeout",
                RepositoryTestData.BaseUtc.AddMinutes(5));
        }

        if (status == CityStatus.Archived)
            city.Archive(RepositoryTestData.BaseUtc.AddHours(1));

        return city;
    }
}
