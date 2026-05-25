using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ListCities
{
    public sealed class ListCitiesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsCitiesAndPassesIncludeArchivedFlag()
        {
            City activeCity = ClassicCityTestSupport.CreateCity();
            City archivedCity = ClassicCityTestSupport.CreateCity("Beta City");
            archivedCity.Archive(ClassicCityTestSupport.CreatedAtUtc.AddHours(3));

            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                Cities =
                [
                    activeCity,
                    archivedCity
                ]
            };
            var handler = new ListCitiesQueryHandler(cityRepository);

            IReadOnlyList<CityDto> result = await handler.Handle(
                request: new ListCitiesQuery(true),
                cancellationToken: CancellationToken.None);

            Assert.True(cityRepository.RequestedIncludeArchived);
            Assert.Equal(
                expected: 2,
                actual: result.Count);
            Assert.Equal(
                expected: "Alpha City",
                actual: result[0].Name);
            Assert.False(result[0].IsArchived);
            Assert.Equal(
                expected: "Beta City",
                actual: result[1].Name);
            Assert.True(result[1].IsArchived);
        }
    }
}
