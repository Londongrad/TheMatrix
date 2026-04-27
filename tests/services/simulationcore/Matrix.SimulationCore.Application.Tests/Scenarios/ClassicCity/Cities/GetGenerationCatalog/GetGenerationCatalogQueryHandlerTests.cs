using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetGenerationCatalog;

public sealed class GetGenerationCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPresetCollectionsFromCatalog()
    {
        var catalog = new ClassicCityTestSupport.FakeCityGenerationContentCatalog
        {
            CityNamePresets = ["Alpha", "Beta"],
            DistrictNamePresets = ["Central", "Harbor"],
            StreetNamePresets = ["Main", "Market"]
        };
        var handler = new GetGenerationCatalogQueryHandler(catalog);

        var result = await handler.Handle(new GetGenerationCatalogQuery(), CancellationToken.None);

        Assert.Equal(["Alpha", "Beta"], result.CityNamePresets);
        Assert.Equal(["Central", "Harbor"], result.DistrictNamePresets);
        Assert.Equal(["Main", "Market"], result.StreetNamePresets);
    }
}
