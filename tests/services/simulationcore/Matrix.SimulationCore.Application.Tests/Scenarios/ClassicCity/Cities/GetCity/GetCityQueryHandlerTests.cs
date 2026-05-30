using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetCity
{
    public sealed class GetCityQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsNull()
        {
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var handler = new GetCityQueryHandler(cityRepository);

            CityDto? result = await handler.Handle(
                request: new GetCityQuery(Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WhenCityExists_ReturnsMappedDto()
        {
            City city = ClassicCityTestSupport.CreateCity("Neo Tokyo");
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var handler = new GetCityQueryHandler(cityRepository);

            CityDto? result = await handler.Handle(
                request: new GetCityQuery(city.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: city.Id.Value,
                actual: result.CityId);
            Assert.Equal(
                expected: city.Id.Value,
                actual: result.SimulationId);
            Assert.Equal(
                expected: city.Name.Value,
                actual: result.Name);
            Assert.Equal(
                expected: SimulationKind.ClassicCity.ToString(),
                actual: result.SimulationKind);
            Assert.Equal(
                expected: city.Status.ToString(),
                actual: result.Status);
            Assert.Equal(
                expected: city.Environment.ClimateZone.ToString(),
                actual: result.ClimateZone);
            Assert.Equal(
                expected: city.Environment.Hemisphere.ToString(),
                actual: result.Hemisphere);
            Assert.Equal(
                expected: city.Environment.UtcOffset.TotalMinutes,
                actual: result.UtcOffsetMinutes);
            Assert.Equal(
                expected: city.GenerationProfile.PlannedPeopleCount,
                actual: result.PlannedPeopleCount);
            Assert.Equal(
                expected: city.PopulationBootstrapOperationId,
                actual: result.PopulationBootstrapOperationId);
            Assert.Equal(
                expected: city.EconomyBootstrapOperationId,
                actual: result.EconomyBootstrapOperationId);
            Assert.Equal(
                expected: city.CreatedAtUtc,
                actual: result.CreatedAtUtc);
            Assert.False(result.IsArchived);
        }
    }
}
