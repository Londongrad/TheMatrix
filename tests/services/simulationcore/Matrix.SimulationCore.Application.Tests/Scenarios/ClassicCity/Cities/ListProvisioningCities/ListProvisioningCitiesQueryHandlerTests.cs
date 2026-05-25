using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListProvisioningCities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ListProvisioningCities
{
    public sealed class ListProvisioningCitiesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsProvisioningCities()
        {
            City city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                ProvisioningCities = [city]
            };
            var handler = new ListProvisioningCitiesQueryHandler(cityRepository);

            IReadOnlyList<CityDto> result = await handler.Handle(
                request: new ListProvisioningCitiesQuery(),
                cancellationToken: CancellationToken.None);

            Assert.True(cityRepository.ListProvisioningRequested);
            CityDto item = Assert.Single(result);
            Assert.Equal(
                expected: city.Id.Value,
                actual: item.CityId);
            Assert.Equal(
                expected: city.Name.Value,
                actual: item.Name);
            Assert.Equal(
                expected: city.Status.ToString(),
                actual: item.Status);
        }
    }
}
