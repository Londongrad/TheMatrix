using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListProvisioningCities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ListProvisioningCities;

public sealed class ListProvisioningCitiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsProvisioningCities()
    {
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            ProvisioningCities = [city]
        };
        var handler = new ListProvisioningCitiesQueryHandler(cityRepository);

        var result = await handler.Handle(new ListProvisioningCitiesQuery(), CancellationToken.None);

        Assert.True(cityRepository.ListProvisioningRequested);
        var item = Assert.Single(result);
        Assert.Equal(city.Id.Value, item.CityId);
        Assert.Equal(city.Name.Value, item.Name);
        Assert.Equal(city.Status.ToString(), item.Status);
    }
}
