using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.GetEmploymentCatalog;

public sealed class GetCityEmploymentCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDistinctSortedJobTitlesAndMappedWorkplaces()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
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
            new GetCityEmploymentCatalogQuery(cityId),
            CancellationToken.None);

        Assert.Equal(CityId.From(cityId), personReadRepository.RequestedCityId);
        Assert.Equal(["Engineer", "teacher"], result.JobTitles);
        Assert.Equal(2, result.CurrentWorkplaces.Count);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), result.CurrentWorkplaces[0].WorkplaceId);
        Assert.Equal(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"), result.CurrentWorkplaces[0].WorkplaceAnchorId);
        Assert.Equal("Engineer", result.CurrentWorkplaces[0].JobTitle);
        Assert.Equal(9, result.CurrentWorkplaces[0].ResidentCount);
        Assert.Null(result.CurrentWorkplaces[1].WorkplaceAnchorId);
        Assert.Equal("Teacher", result.CurrentWorkplaces[1].JobTitle);
    }

    private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
        public IReadOnlyList<string> FemaleFirstNames => ["Anna"];

        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
        [
            new("Ivanov", "Ivanova")
        ];

        public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
        [
            new("teacher", 1),
            new("Engineer", 1),
            new("Teacher", 1)
        ];
    }
}
