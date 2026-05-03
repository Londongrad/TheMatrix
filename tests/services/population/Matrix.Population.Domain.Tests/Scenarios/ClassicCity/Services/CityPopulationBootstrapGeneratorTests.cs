using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationBootstrapGeneratorTests
{
    [Fact]
    public void GenerateStandalone_WhenPeopleCountIsNonPositive_ReturnsEmpty()
    {
        var generator = CreateGenerator();

        PopulationBootstrapResult result = generator.GenerateStandalone(
            peopleCount: 0,
            currentDate: new DateOnly(2048, 5, 3),
            createdAtUtc: new DateTimeOffset(2048, 5, 3, 0, 0, 0, TimeSpan.Zero),
            randomSeed: 42);

        Assert.Empty(result.Households);
        Assert.Empty(result.HouseholdPlacements);
        Assert.Empty(result.Persons);
    }

    [Fact]
    public void GenerateStandalone_WhenPeopleCountIsPositive_CreatesOneResidentPerHousehold()
    {
        var generator = CreateGenerator();
        DateTimeOffset createdAtUtc = new(2048, 5, 3, 0, 0, 0, TimeSpan.Zero);

        PopulationBootstrapResult result = generator.GenerateStandalone(
            peopleCount: 3,
            currentDate: new DateOnly(2048, 5, 3),
            createdAtUtc: createdAtUtc,
            randomSeed: 17);

        Assert.Equal(3, result.Households.Count);
        Assert.Empty(result.HouseholdPlacements);
        Assert.Equal(3, result.Persons.Count);
        Assert.All(result.Households, household =>
        {
            Assert.Equal(1, household.Size.Value);
            Assert.Equal(createdAtUtc, household.CreatedAtUtc);
        });

        HashSet<HouseholdId> householdIds = result.Households.Select(x => x.Id).ToHashSet();
        Assert.Equal(3, householdIds.Count);
        Assert.All(result.Persons, person => Assert.Contains(person.HouseholdId, householdIds));
    }

    [Fact]
    public void GenerateForCity_WhenNoResidentialCapacityExists_CreatesHomelessPlacements()
    {
        var generator = CreateGenerator();
        CityId cityId = CityId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));

        PopulationBootstrapResult result = generator.GenerateForCity(
            cityId: cityId,
            residentialBuildings: Array.Empty<ResidentialBuildingResidence>(),
            cityAnchors: Array.Empty<CityPopulationAnchorCatalogItem>(),
            peopleCount: 5,
            currentDate: new DateOnly(2048, 5, 3),
            createdAtUtc: new DateTimeOffset(2048, 5, 3, 0, 0, 0, TimeSpan.Zero),
            tuning: CityPopulationBootstrapTuning.Default(),
            randomSeed: 7);

        Assert.Equal(5, result.Persons.Count);
        Assert.Equal(result.Households.Count, result.HouseholdPlacements.Count);
        Assert.All(result.HouseholdPlacements, placement =>
        {
            Assert.Equal(cityId, placement.CityId);
            Assert.Equal(HousingStatus.Homeless, placement.HousingStatus);
            Assert.Null(placement.DistrictId);
            Assert.Null(placement.ResidentialBuildingId);
        });
        Assert.Equal(5, result.Households.Sum(x => x.Size.Value));
    }

    [Fact]
    public void GenerateForCity_WhenCapacityIsSufficient_HousesEveryGeneratedHousehold()
    {
        var generator = CreateGenerator();
        CityId cityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        DistrictId districtId = DistrictId.From(Guid.Parse("12121212-3434-5656-7878-909090909090"));
        ResidentialBuildingId buildingId = ResidentialBuildingId.From(Guid.Parse("23232323-4545-6767-8989-010101010101"));
        PopulationBootstrapResult result = generator.GenerateForCity(
            cityId: cityId,
            residentialBuildings:
            [
                new ResidentialBuildingResidence(
                    residentialBuildingId: buildingId,
                    districtId: districtId,
                    residentCapacity: 12)
            ],
            cityAnchors: Array.Empty<CityPopulationAnchorCatalogItem>(),
            peopleCount: 6,
            currentDate: new DateOnly(2048, 5, 3),
            createdAtUtc: new DateTimeOffset(2048, 5, 3, 0, 0, 0, TimeSpan.Zero),
            tuning: CityPopulationBootstrapTuning.Default(),
            randomSeed: 5);

        Assert.Equal(6, result.Persons.Count);
        Assert.Equal(result.Households.Count, result.HouseholdPlacements.Count);
        Assert.All(result.HouseholdPlacements, placement =>
        {
            Assert.Equal(HousingStatus.Housed, placement.HousingStatus);
            Assert.Equal(cityId, placement.CityId);
            Assert.Equal(districtId, placement.DistrictId);
            Assert.Equal(buildingId, placement.ResidentialBuildingId);
        });

        Dictionary<HouseholdId, int> residentCountsByHousehold = result.Persons
            .GroupBy(x => x.HouseholdId)
            .ToDictionary(x => x.Key, x => x.Count());

        Assert.All(result.Households, household =>
        {
            Assert.True(residentCountsByHousehold.TryGetValue(household.Id, out int residentCount));
            Assert.Equal(household.Size.Value, residentCount);
        });
        Assert.Equal(6, result.Households.Sum(x => x.Size.Value));
    }

    private static CityPopulationBootstrapGenerator CreateGenerator()
    {
        return new CityPopulationBootstrapGenerator(
            contentCatalog: new TestPopulationGenerationContentCatalog(),
            anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
    }

    private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => ["Ivan", "Pavel"];
        public IReadOnlyList<string> FemaleFirstNames => ["Anna", "Maria"];
        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
        [
            new PopulationFamilySurnameCatalogItem("Ivanov", "Ivanova"),
            new PopulationFamilySurnameCatalogItem("Petrov", "Petrova")
        ];

        public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
        [
            new PopulationProfessionCatalogItem("Engineer", 1),
            new PopulationProfessionCatalogItem("Teacher", 1)
        ];
    }
}
