using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationHappyPathTests
    {
        [Fact]
        public async Task Handle_WhenResidentialCapacityExists_PersistsBootstrapAndReturnsSummary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personWriteRepository = new FakePersonWriteRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            InitializeCityPopulationCommandHandler handler = CreateHandler(
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                environmentRepository: environmentRepository,
                anchorCatalogRepository: anchorCatalogRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                outboxWriter: outboxWriter,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                unitOfWork: unitOfWork);

            CityPopulationBootstrapSummaryDto result = await handler.Handle(
                request: CreateHousedCommand(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: result.CityId);
            Assert.Equal(
                expected: 6,
                actual: result.RequestedPeopleCount);
            Assert.Equal(
                expected: 6,
                actual: result.GeneratedPeopleCount);
            Assert.True(result.HouseholdCount > 0);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: result.HousedHouseholdCount);
            Assert.Equal(
                expected: 0,
                actual: result.HomelessHouseholdCount);
            Assert.Equal(
                expected: 6,
                actual: result.HousedPeopleCount);
            Assert.Equal(
                expected: 0,
                actual: result.HomelessPeopleCount);

            Assert.Single(environmentRepository.UpsertedEnvironments);
            Assert.Equal(
                expected: 1,
                actual: anchorCatalogRepository.DeleteByCityCalls);
            Assert.Single(anchorCatalogRepository.AddedRanges);
            Assert.Equal(
                expected: 2,
                actual: anchorCatalogRepository.AddedRanges[0].Count);
            Assert.Equal(
                expected: 1,
                actual: householdWriteRepository.DeleteByCityCalls);

            (IReadOnlyCollection<Household> Households, IReadOnlyCollection<ClassicCityHouseholdPlacement> Placements)
                addedHouseholds = Assert.Single(householdWriteRepository.AddedRanges);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: addedHouseholds.Households.Count);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: addedHouseholds.Placements.Count);
            Assert.All(
                collection: addedHouseholds.Placements,
                action: x => Assert.Equal(
                    expected: HousingStatus.Housed,
                    actual: x.HousingStatus));

            IReadOnlyCollection<Person> addedPersons = Assert.Single(personWriteRepository.AddedRanges);
            Assert.Equal(
                expected: result.GeneratedPeopleCount,
                actual: addedPersons.Count);

            (Domain.Scenarios.ClassicCity.ValueObjects.CityId CityId, DateOnly CurrentDate, int PersonCount, int
                PlacementCount, bool IncludeCommuteMetrics) updateCall =
                    Assert.Single(summaryProjectionService.UpdateCalls);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: updateCall.CityId);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                actual: updateCall.CurrentDate);
            Assert.Equal(
                expected: result.GeneratedPeopleCount,
                actual: updateCall.PersonCount);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: updateCall.PlacementCount);
            Assert.False(updateCall.IncludeCommuteMetrics);

            CityPopulationActivityWriteModel activity = Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.PopulationInitialized,
                actual: activity.EventType);
            Assert.Equal(
                expected: cityId,
                actual: activity.CityId);

            Assert.NotEmpty(outboxWriter.HouseholdBatches);
            Assert.All(
                collection: outboxWriter.HouseholdBatches,
                action: batch =>
                {
                    Assert.Equal(
                        expected: cityId,
                        actual: batch.CityId);
                    Assert.All(
                        collection: batch.Households,
                        action: item => Assert.True(item.IsHoused));
                });

            var residentFactsBatch = Assert.Single(residentFactsOutboxWriter.Batches);
            Assert.Equal(cityId, residentFactsBatch.SimulationHostId);
            Assert.Equal(0, residentFactsBatch.SourceRevision);
            Assert.Equal(result.GeneratedPeopleCount, residentFactsBatch.Residents.Count);
            Assert.Equal(UtcNow, residentFactsBatch.SynchronizedAtUtc);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenNoResidentialCapacityExists_ProducesHomelessBootstrap()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var personWriteRepository = new FakePersonWriteRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            InitializeCityPopulationCommandHandler handler = CreateHandler(
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                outboxWriter: outboxWriter);

            CityPopulationBootstrapSummaryDto result = await handler.Handle(
                request: CreateHomelessCommand(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 5,
                actual: result.RequestedPeopleCount);
            Assert.Equal(
                expected: 5,
                actual: result.GeneratedPeopleCount);
            Assert.Equal(
                expected: 0,
                actual: result.HousedHouseholdCount);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: result.HomelessHouseholdCount);
            Assert.Equal(
                expected: 0,
                actual: result.HousedPeopleCount);
            Assert.Equal(
                expected: result.GeneratedPeopleCount,
                actual: result.HomelessPeopleCount);

            (IReadOnlyCollection<Household> Households, IReadOnlyCollection<ClassicCityHouseholdPlacement> Placements)
                addedHouseholds = Assert.Single(householdWriteRepository.AddedRanges);
            Assert.Equal(
                expected: result.HouseholdCount,
                actual: addedHouseholds.Households.Count);
            Assert.All(
                collection: addedHouseholds.Placements,
                action: x => Assert.Equal(
                    expected: HousingStatus.Homeless,
                    actual: x.HousingStatus));

            Assert.NotEmpty(outboxWriter.HouseholdBatches);
            Assert.All(
                collection: outboxWriter.HouseholdBatches,
                action: batch =>
                {
                    Assert.Equal(
                        expected: cityId,
                        actual: batch.CityId);
                    Assert.All(
                        collection: batch.Households,
                        action: item => Assert.False(item.IsHoused));
                });

            IReadOnlyCollection<Person> addedPersons = Assert.Single(personWriteRepository.AddedRanges);
            Assert.Equal(
                expected: result.GeneratedPeopleCount,
                actual: addedPersons.Count);
        }

        private static InitializeCityPopulationCommandHandler CreateHandler(
            FakePersonWriteRepository? personWriteRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeCityPopulationEnvironmentRepository? environmentRepository = null,
            FakeCityPopulationAnchorCatalogRepository? anchorCatalogRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityEconomySettlementOutboxWriter? outboxWriter = null,
            FakePopulationResidentFactsOutboxWriter? residentFactsOutboxWriter = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new InitializeCityPopulationCommandHandler(
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                cityPopulationArchiveStateRepository: new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: new FakeCityPopulationDeletionStateRepository(),
                cityPopulationEnvironmentRepository: environmentRepository ??
                                                     new FakeCityPopulationEnvironmentRepository(),
                cityPopulationAnchorCatalogRepository: anchorCatalogRepository ??
                                                       new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                cityEconomySettlementOutboxWriter: outboxWriter ?? new FakeCityEconomySettlementOutboxWriter(),
                residentFactsOutboxWriter: residentFactsOutboxWriter ??
                                           new FakePopulationResidentFactsOutboxWriter(),
                generator: new CityPopulationBootstrapGenerator(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy()),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static InitializeCityPopulationCommand CreateHousedCommand(Guid cityId)
        {
            return new InitializeCityPopulationCommand(
                CityId: cityId,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
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
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
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
