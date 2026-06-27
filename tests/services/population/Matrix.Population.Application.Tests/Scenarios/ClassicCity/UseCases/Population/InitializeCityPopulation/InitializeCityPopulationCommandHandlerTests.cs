using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class InitializeCityPopulationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCityIsArchived_ThrowsConflictWithoutMutatingState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository();
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            InitializeCityPopulationCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                deletionStateRepository: deletionStateRepository,
                environmentRepository: environmentRepository,
                anchorCatalogRepository: anchorCatalogRepository,
                householdWriteRepository: householdWriteRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                outboxWriter: outboxWriter,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: CreateCommand(cityId),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.City.Archived",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: archiveStateRepository.RequestedCityId);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: deletionStateRepository.RequestedCityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(environmentRepository.UpsertedEnvironments);
            Assert.Equal(
                expected: 0,
                actual: anchorCatalogRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 0,
                actual: householdWriteRepository.DeleteByCityCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(outboxWriter.HouseholdBatches);
            Assert.Empty(outboxWriter.WorkplaceBatches);
            Assert.Empty(residentFactsOutboxWriter.Batches);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ThrowsConflictWithoutMutatingState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository();
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow.AddDays(-2),
                    updatedAtUtc: UtcNow.AddDays(-1))
            };
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var residentFactsOutboxWriter = new FakePopulationResidentFactsOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            InitializeCityPopulationCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                deletionStateRepository: deletionStateRepository,
                environmentRepository: environmentRepository,
                anchorCatalogRepository: anchorCatalogRepository,
                householdWriteRepository: householdWriteRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                outboxWriter: outboxWriter,
                residentFactsOutboxWriter: residentFactsOutboxWriter,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: CreateCommand(cityId),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.City.Deleted",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Conflict,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: archiveStateRepository.RequestedCityId);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: deletionStateRepository.RequestedCityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            Assert.Empty(environmentRepository.UpsertedEnvironments);
            Assert.Equal(
                expected: 0,
                actual: anchorCatalogRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 0,
                actual: householdWriteRepository.DeleteByCityCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(outboxWriter.HouseholdBatches);
            Assert.Empty(outboxWriter.WorkplaceBatches);
            Assert.Empty(residentFactsOutboxWriter.Batches);
        }

        private static InitializeCityPopulationCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationEnvironmentRepository? environmentRepository = null,
            FakeCityPopulationAnchorCatalogRepository? anchorCatalogRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityEconomySettlementOutboxWriter? outboxWriter = null,
            FakePopulationResidentFactsOutboxWriter? residentFactsOutboxWriter = null,
            FakePopulationResidentMedicalStateOutboxWriter? residentMedicalStateOutboxWriter = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new InitializeCityPopulationCommandHandler(
                personWriteRepository: new FakePersonWriteRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
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
                residentMedicalStateOutboxWriter: residentMedicalStateOutboxWriter ??
                                                  new FakePopulationResidentMedicalStateOutboxWriter(),
                generator: null!,
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static InitializeCityPopulationCommand CreateCommand(Guid cityId)
        {
            return new InitializeCityPopulationCommand(
                CityId: cityId,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                CreatedAtUtc: UtcNow,
                PeopleCount: 100,
                RandomSeed: 11,
                Environment: new CityPopulationEnvironmentInput(
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180),
                Tuning: new CityPopulationBootstrapTuningInput(
                    HousingPressurePercent: 30,
                    EconomicStabilityPercent: 55,
                    SocialVolatilityPercent: 20,
                    FamilyFormationPercent: 35),
                CityAnchors:
                [
                    new CityAnchorSeedItem(
                        CityAnchorId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        AccessRoadNodeId: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                        Name: "Central Clinic",
                        Type: "Hospital",
                        Capacity: 80,
                        PositionX: 10m,
                        PositionY: 20m,
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
    }
}
