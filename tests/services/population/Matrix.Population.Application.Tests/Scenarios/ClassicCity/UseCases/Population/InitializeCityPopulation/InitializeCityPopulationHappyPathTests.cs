using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;

public sealed class InitializeCityPopulationHappyPathTests
{
    [Fact]
    public async Task Handle_WhenResidentialCapacityExists_PersistsBootstrapAndReturnsSummary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personWriteRepository = new FakePersonWriteRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        var environmentRepository = new FakeCityPopulationEnvironmentRepository();
        var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personWriteRepository: personWriteRepository,
            householdWriteRepository: householdWriteRepository,
            environmentRepository: environmentRepository,
            anchorCatalogRepository: anchorCatalogRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            outboxWriter: outboxWriter,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(CreateHousedCommand(cityId), CancellationToken.None);

        Assert.Equal(cityId, result.CityId);
        Assert.Equal(6, result.RequestedPeopleCount);
        Assert.Equal(6, result.GeneratedPeopleCount);
        Assert.True(result.HouseholdCount > 0);
        Assert.Equal(result.HouseholdCount, result.HousedHouseholdCount);
        Assert.Equal(0, result.HomelessHouseholdCount);
        Assert.Equal(6, result.HousedPeopleCount);
        Assert.Equal(0, result.HomelessPeopleCount);

        Assert.Single(environmentRepository.UpsertedEnvironments);
        Assert.Equal(1, anchorCatalogRepository.DeleteByCityCalls);
        Assert.Single(anchorCatalogRepository.AddedRanges);
        Assert.Equal(2, anchorCatalogRepository.AddedRanges[0].Count);
        Assert.Equal(1, householdWriteRepository.DeleteByCityCalls);

        var addedHouseholds = Assert.Single(householdWriteRepository.AddedRanges);
        Assert.Equal(result.HouseholdCount, addedHouseholds.Households.Count);
        Assert.Equal(result.HouseholdCount, addedHouseholds.Placements.Count);
        Assert.All(addedHouseholds.Placements, x => Assert.Equal(HousingStatus.Housed, x.HousingStatus));

        IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> addedPersons = Assert.Single(personWriteRepository.AddedRanges);
        Assert.Equal(result.GeneratedPeopleCount, addedPersons.Count);

        var updateCall = Assert.Single(summaryProjectionService.UpdateCalls);
        Assert.Equal(CityId.From(cityId), updateCall.CityId);
        Assert.Equal(new DateOnly(2048, 5, 3), updateCall.CurrentDate);
        Assert.Equal(result.GeneratedPeopleCount, updateCall.PersonCount);
        Assert.Equal(result.HouseholdCount, updateCall.PlacementCount);
        Assert.False(updateCall.IncludeCommuteMetrics);

        var activity = Assert.Single(activityJournalService.Entries);
        Assert.Equal(CityPopulationActivityEventType.PopulationInitialized, activity.EventType);
        Assert.Equal(cityId, activity.CityId);

        Assert.NotEmpty(outboxWriter.HouseholdBatches);
        Assert.All(outboxWriter.HouseholdBatches, batch =>
        {
            Assert.Equal(cityId, batch.CityId);
            Assert.All(batch.Households, item => Assert.True(item.IsHoused));
        });

        Assert.Equal(1, unitOfWork.ExecuteTransactionCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenNoResidentialCapacityExists_ProducesHomelessBootstrap()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var personWriteRepository = new FakePersonWriteRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
        var handler = CreateHandler(
            personWriteRepository: personWriteRepository,
            householdWriteRepository: householdWriteRepository,
            outboxWriter: outboxWriter);

        var result = await handler.Handle(CreateHomelessCommand(cityId), CancellationToken.None);

        Assert.Equal(5, result.RequestedPeopleCount);
        Assert.Equal(5, result.GeneratedPeopleCount);
        Assert.Equal(0, result.HousedHouseholdCount);
        Assert.Equal(result.HouseholdCount, result.HomelessHouseholdCount);
        Assert.Equal(0, result.HousedPeopleCount);
        Assert.Equal(result.GeneratedPeopleCount, result.HomelessPeopleCount);

        var addedHouseholds = Assert.Single(householdWriteRepository.AddedRanges);
        Assert.Equal(result.HouseholdCount, addedHouseholds.Households.Count);
        Assert.All(addedHouseholds.Placements, x => Assert.Equal(HousingStatus.Homeless, x.HousingStatus));

        Assert.NotEmpty(outboxWriter.HouseholdBatches);
        Assert.All(outboxWriter.HouseholdBatches, batch =>
        {
            Assert.Equal(cityId, batch.CityId);
            Assert.All(batch.Households, item => Assert.False(item.IsHoused));
        });

        IReadOnlyCollection<Matrix.Population.Domain.Entities.Person> addedPersons = Assert.Single(personWriteRepository.AddedRanges);
        Assert.Equal(result.GeneratedPeopleCount, addedPersons.Count);
    }

    private static InitializeCityPopulationCommandHandler CreateHandler(
        FakePersonWriteRepository? personWriteRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeCityPopulationEnvironmentRepository? environmentRepository = null,
        FakeCityPopulationAnchorCatalogRepository? anchorCatalogRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakeCityEconomySettlementOutboxWriter? outboxWriter = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new InitializeCityPopulationCommandHandler(
            personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
            householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            cityPopulationArchiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
            cityPopulationDeletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
            cityPopulationEnvironmentRepository: environmentRepository ?? new FakeCityPopulationEnvironmentRepository(),
            cityPopulationAnchorCatalogRepository: anchorCatalogRepository ?? new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationActivityJournalService: activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            cityPopulationSummaryProjectionService: summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            cityEconomySettlementOutboxWriter: outboxWriter ?? new FakeCityEconomySettlementOutboxWriter(),
            generator: new CityPopulationBootstrapGenerator(
                contentCatalog: new TestPopulationGenerationContentCatalog(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy()),
            unitOfWork: unitOfWork ?? new FakeUnitOfWork());
    }

    private static InitializeCityPopulationCommand CreateHousedCommand(Guid cityId)
    {
        return new InitializeCityPopulationCommand(
            CityId: cityId,
            CurrentDate: new DateOnly(2048, 5, 3),
            CreatedAtUtc: UtcNow,
            PeopleCount: 6,
            RandomSeed: 5,
            Environment: new CityPopulationEnvironmentInput(
                ClimateZone: "Temperate",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180),
            Tuning: new CityPopulationBootstrapTuningInput(
                HousingPressurePercent: 40,
                EconomicStabilityPercent: 60,
                SocialVolatilityPercent: 25,
                FamilyFormationPercent: 55),
            CityAnchors:
            [
                new CityAnchorSeedItem(
                    CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    Name: "Factory",
                    Type: "Workplace",
                    Capacity: 120,
                    PositionX: 10m,
                    PositionY: 20m,
                    CreatedAtUtc: UtcNow),
                new CityAnchorSeedItem(
                    CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    Name: "School",
                    Type: "School",
                    Capacity: 80,
                    PositionX: 11m,
                    PositionY: 21m,
                    CreatedAtUtc: UtcNow)
            ],
            ResidentialBuildings:
            [
                new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: Guid.Parse("40000000-0000-0000-0000-000000000001"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    ResidentCapacity: 12)
            ]);
    }

    private static InitializeCityPopulationCommand CreateHomelessCommand(Guid cityId)
    {
        return new InitializeCityPopulationCommand(
            CityId: cityId,
            CurrentDate: new DateOnly(2048, 5, 3),
            CreatedAtUtc: UtcNow,
            PeopleCount: 5,
            RandomSeed: 7,
            Environment: new CityPopulationEnvironmentInput(
                ClimateZone: "Continental",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180),
            Tuning: new CityPopulationBootstrapTuningInput(
                HousingPressurePercent: 65,
                EconomicStabilityPercent: 45,
                SocialVolatilityPercent: 30,
                FamilyFormationPercent: 40),
            CityAnchors:
            [
                new CityAnchorSeedItem(
                    CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000003"),
                    Name: "Clinic",
                    Type: "Hospital",
                    Capacity: 60,
                    PositionX: 12m,
                    PositionY: 22m,
                    CreatedAtUtc: UtcNow)
            ],
            ResidentialBuildings: Array.Empty<ResidentialBuildingSeedItem>());
    }

    private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
    {
        public IReadOnlyList<string> MaleFirstNames => ["Ivan", "Pavel"];
        public IReadOnlyList<string> FemaleFirstNames => ["Anna", "Maria"];

        public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
        [
            new("Ivanov", "Ivanova"),
            new("Petrov", "Petrova")
        ];

        public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
        [
            new("Engineer", 1),
            new("Teacher", 1)
        ];
    }
}
