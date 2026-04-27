using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.ListCities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.ListCities;

public sealed class ListCitiesQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsCitiesAndPassesIncludeArchivedFlag()
    {
        var activeCity = ClassicCityTestSupport.CreateCity("Alpha City");
        var archivedCity = ClassicCityTestSupport.CreateCity("Beta City");
        archivedCity.Archive(ClassicCityTestSupport.CreatedAtUtc.AddHours(3));

        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            Cities = [activeCity, archivedCity]
        };
        var handler = new ListCitiesQueryHandler(cityRepository);

        var result = await handler.Handle(new ListCitiesQuery(true), CancellationToken.None);

        Assert.True(cityRepository.RequestedIncludeArchived);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha City", result[0].Name);
        Assert.False(result[0].IsArchived);
        Assert.Equal("Beta City", result[1].Name);
        Assert.True(result[1].IsArchived);
    }
}
