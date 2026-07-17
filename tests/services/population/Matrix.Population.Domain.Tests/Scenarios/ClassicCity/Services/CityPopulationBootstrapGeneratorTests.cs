using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationBootstrapGeneratorTests
    {
        [Fact]
        public void GenerateStandalone_WhenPeopleCountIsNonPositive_ReturnsEmpty()
        {
            CityPopulationBootstrapGenerator generator = CreateGenerator();

            PopulationBootstrapResult result = generator.GenerateStandalone(
                peopleCount: 0,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                randomSeed: 42);

            Assert.Empty(result.Households);
            Assert.Empty(result.HouseholdPlacements);
            Assert.Empty(result.Persons);
        }

        [Fact]
        public void GenerateStandalone_WhenPeopleCountIsPositive_CreatesOneResidentPerHousehold()
        {
            CityPopulationBootstrapGenerator generator = CreateGenerator();
            DateTimeOffset createdAtUtc = new(
                year: 2048,
                month: 5,
                day: 3,
                hour: 0,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            PopulationBootstrapResult result = generator.GenerateStandalone(
                peopleCount: 3,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                createdAtUtc: createdAtUtc,
                randomSeed: 17);

            Assert.Equal(
                expected: 3,
                actual: result.Households.Count);
            Assert.Empty(result.HouseholdPlacements);
            Assert.Equal(
                expected: 3,
                actual: result.Persons.Count);
            Assert.All(
                collection: result.Households,
                action: household =>
                {
                    Assert.Equal(
                        expected: 1,
                        actual: household.Size.Value);
                    Assert.Equal(
                        expected: createdAtUtc,
                        actual: household.CreatedAtUtc);
                });

            var householdIds = result.Households.Select(x => x.Id)
               .ToHashSet();
            Assert.Equal(
                expected: 3,
                actual: householdIds.Count);
            Assert.All(
                collection: result.Persons,
                action: person =>
                {
                    Assert.Contains(
                        expected: person.HouseholdId,
                        set: householdIds);
                    Assert.NotEqual(
                        expected: EmploymentStatus.Student,
                        actual: person.Employment.Status);
                    Assert.Null(person.Education.CurrentInstitutionId);
                    Assert.Null(person.Education.CurrentInstitutionAnchorId);
                });
        }

        [Fact]
        public void GenerateForCity_WhenNoResidentialCapacityExists_CreatesHomelessPlacements()
        {
            CityPopulationBootstrapGenerator generator = CreateGenerator();
            var cityId = CityId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));

            PopulationBootstrapResult result = generator.GenerateForCity(
                cityId: cityId,
                residentialBuildings: Array.Empty<ResidentialBuildingResidence>(),
                cityAnchors: Array.Empty<CityPopulationAnchorCatalogItem>(),
                peopleCount: 5,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                tuning: CityPopulationBootstrapTuning.Default(),
                randomSeed: 7);

            Assert.Equal(
                expected: 5,
                actual: result.Persons.Count);
            Assert.Equal(
                expected: result.Households.Count,
                actual: result.HouseholdPlacements.Count);
            Assert.All(
                collection: result.HouseholdPlacements,
                action: placement =>
                {
                    Assert.Equal(
                        expected: cityId,
                        actual: placement.CityId);
                    Assert.Equal(
                        expected: HousingStatus.Homeless,
                        actual: placement.HousingStatus);
                    Assert.Null(placement.DistrictId);
                    Assert.Null(placement.ResidentialBuildingId);
                });
            Assert.Equal(
                expected: 5,
                actual: result.Households.Sum(x => x.Size.Value));
        }

        [Fact]
        public void GenerateForCity_WhenCapacityIsSufficient_HousesEveryGeneratedHousehold()
        {
            CityPopulationBootstrapGenerator generator = CreateGenerator();
            var cityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
            var districtId = DistrictId.From(Guid.Parse("12121212-3434-5656-7878-909090909090"));
            var buildingId = ResidentialBuildingId.From(Guid.Parse("23232323-4545-6767-8989-010101010101"));
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
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 3,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                tuning: CityPopulationBootstrapTuning.Default(),
                randomSeed: 5);

            Assert.Equal(
                expected: 6,
                actual: result.Persons.Count);
            Assert.Equal(
                expected: result.Households.Count,
                actual: result.HouseholdPlacements.Count);
            Assert.All(
                collection: result.HouseholdPlacements,
                action: placement =>
                {
                    Assert.Equal(
                        expected: HousingStatus.Housed,
                        actual: placement.HousingStatus);
                    Assert.Equal(
                        expected: cityId,
                        actual: placement.CityId);
                    Assert.Equal(
                        expected: districtId,
                        actual: placement.DistrictId);
                    Assert.Equal(
                        expected: buildingId,
                        actual: placement.ResidentialBuildingId);
                });

            var residentCountsByHousehold = result.Persons
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Count());

            Assert.All(
                collection: result.Households,
                action: household =>
                {
                    Assert.True(
                        residentCountsByHousehold.TryGetValue(
                            key: household.Id,
                            value: out int residentCount));
                    Assert.Equal(
                        expected: household.Size.Value,
                        actual: residentCount);
                });
            Assert.Equal(
                expected: 6,
                actual: result.Households.Sum(x => x.Size.Value));
        }

        private static CityPopulationBootstrapGenerator CreateGenerator()
        {
            return new CityPopulationBootstrapGenerator(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy());
        }

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames =>
            [
                "Ivan",
                "Pavel"
            ];

            public IReadOnlyList<string> FemaleFirstNames =>
            [
                "Anna",
                "Maria"
            ];

            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
            [
                new(
                    Masculine: "Ivanov",
                    Feminine: "Ivanova"),
                new(
                    Masculine: "Petrov",
                    Feminine: "Petrova")
            ];

            public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
            [
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
