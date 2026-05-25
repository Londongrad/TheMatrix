using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities.GetGenerationCatalog
{
    public sealed class GetGenerationCatalogQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsPresetCollectionsFromCatalog()
        {
            var catalog = new ClassicCityTestSupport.FakeCityGenerationContentCatalog
            {
                CityNamePresets =
                [
                    "Alpha",
                    "Beta"
                ],
                DistrictNamePresets =
                [
                    "Central",
                    "Harbor"
                ],
                StreetNamePresets =
                [
                    "Main",
                    "Market"
                ]
            };
            var handler = new GetGenerationCatalogQueryHandler(catalog);

            CityGenerationCatalogDto result = await handler.Handle(
                request: new GetGenerationCatalogQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected:
                [
                    "Alpha",
                    "Beta"
                ],
                actual: result.CityNamePresets);
            Assert.Equal(
                expected:
                [
                    "Central",
                    "Harbor"
                ],
                actual: result.DistrictNamePresets);
            Assert.Equal(
                expected:
                [
                    "Main",
                    "Market"
                ],
                actual: result.StreetNamePresets);
        }
    }
}
