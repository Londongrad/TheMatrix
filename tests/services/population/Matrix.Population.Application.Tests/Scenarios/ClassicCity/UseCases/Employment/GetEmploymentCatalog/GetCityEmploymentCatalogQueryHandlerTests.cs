using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog
{
    public sealed class GetCityEmploymentCatalogQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsDistinctSortedJobTitlesAndMappedWorkplaces()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personReadRepository = new FakeCityPopulationPersonReadRepository
            {
                EmploymentWorkplaces =
                [
                    new CityEmploymentWorkplaceSnapshot(
                        WorkplaceId: WorkplaceId.From(Guid.Parse("11111111-2222-3333-4444-555555555555")),
                        WorkplaceAnchorId: CityAnchorId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
                        JobTitle: "Engineer",
                        ResidentCount: 9),
                    new CityEmploymentWorkplaceSnapshot(
                        WorkplaceId: WorkplaceId.From(Guid.Parse("66666666-7777-8888-9999-000000000000")),
                        WorkplaceAnchorId: null,
                        JobTitle: "Teacher",
                        ResidentCount: 4)
                ]
            };
            var handler = new GetCityEmploymentCatalogQueryHandler(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                cityPopulationPersonReadRepository: personReadRepository);

            CityEmploymentCatalogDto result = await handler.Handle(
                request: new GetCityEmploymentCatalogQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: CityId.From(cityId),
                actual: personReadRepository.RequestedCityId);
            Assert.Equal(
                expected:
                [
                    "Engineer",
                    "teacher"
                ],
                actual: result.JobTitles);
            Assert.Equal(
                expected: 2,
                actual: result.CurrentWorkplaces.Count);
            Assert.Equal(
                expected: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                actual: result.CurrentWorkplaces[0].WorkplaceId);
            Assert.Equal(
                expected: Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                actual: result.CurrentWorkplaces[0].WorkplaceAnchorId);
            Assert.Equal(
                expected: "Engineer",
                actual: result.CurrentWorkplaces[0].JobTitle);
            Assert.Equal(
                expected: 9,
                actual: result.CurrentWorkplaces[0].ResidentCount);
            Assert.Null(result.CurrentWorkplaces[1].WorkplaceAnchorId);
            Assert.Equal(
                expected: "Teacher",
                actual: result.CurrentWorkplaces[1].JobTitle);
        }

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];

            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
            [
                new(
                    Masculine: "Ivanov",
                    Feminine: "Ivanova")
            ];

            public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
            [
                new(
                    Title: "teacher",
                    Weight: 1),
                new(
                    Title: "Engineer",
                    Weight: 1),
                new(
                    Title: "Teacher",
                    Weight: 1)
            ];
        }
    }
}
