using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.HireResident
{
    public sealed class HireCityResidentCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenWorkplaceExists_AssignsEmploymentAndPublishesSyncBatch()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var workplaceId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            var workplaceAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
            Person resident = CreatePerson(
                personId: residentId,
                employmentStatus: EmploymentStatus.Unemployed,
                happiness: 43);
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[resident.Id] = resident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository
            {
                EmploymentWorkplaces =
                [
                    new CityEmploymentWorkplaceSnapshot(
                        WorkplaceId: WorkplaceId.From(workplaceId),
                        WorkplaceAnchorId: CityAnchorId.From(workplaceAnchorId),
                        JobTitle: "Engineer",
                        ResidentCount: 8)
                ]
            };
            cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var personWriteRepository = new FakePersonWriteRepository();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            HireCityResidentCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                cityEconomySettlementOutboxWriter: outboxWriter,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            CityEmploymentOperationResultDto result = await handler.Handle(
                request: new HireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    JobTitle: "Unused",
                    WorkplaceId: workplaceId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "EmploymentAssigned",
                actual: result.Action);
            Assert.Equal(
                expected: EmploymentStatus.Employed,
                actual: resident.Employment.Status);
            Assert.NotNull(resident.Employment.Job);
            Assert.Equal(
                expected: WorkplaceId.From(workplaceId),
                actual: resident.Employment.Job!.WorkplaceId);
            Assert.Equal(
                expected: "Engineer",
                actual: resident.Employment.Job.Title);
            Assert.Equal(
                expected: CityAnchorId.From(workplaceAnchorId),
                actual: resident.Employment.Job.WorkplaceAnchorId);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            ClassicCityWorkplaceBusinessSyncBatchV1 batch = Assert.Single(outboxWriter.WorkplaceBatches);
            ClassicCityWorkplaceBusinessSyncItemV1 workplace = Assert.Single(batch.Workplaces);
            Assert.Equal(
                expected: workplaceId,
                actual: workplace.WorkplaceId);
            Assert.Equal(
                expected: "Engineer",
                actual: workplace.JobTitle);
            Assert.Equal(
                expected: 1,
                actual: workplace.ActiveWorkerCount);
            Assert.Equal(
                expected: "Employed",
                actual: result.Resident.EmploymentStatus);
            Assert.NotNull(result.Resident.CurrentWorkplace);
            Assert.Equal(
                expected: workplaceId,
                actual: result.Resident.CurrentWorkplace!.WorkplaceId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenOnlyJobTitleIsProvided_UsesAnchorCatalogSelection()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var districtId = Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb");
            var anchorId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd");
            Person resident = CreatePerson(
                personId: residentId,
                employmentStatus: EmploymentStatus.Unemployed,
                happiness: 41);
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[resident.Id] = resident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
            cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] =
                new CityResidentHousingSnapshot(
                    HouseholdId: resident.HouseholdId,
                    HousingStatus: HousingStatus.Housed,
                    DistrictId: DistrictId.From(districtId),
                    ResidentialBuildingId: ResidentialBuildingId.From(
                        Guid.Parse("eeeeeeee-1111-2222-3333-ffffffffffff")));
            var anchorCatalogRepository = new FakeCityPopulationAnchorCatalogRepository
            {
                Items =
                [
                    CityPopulationAnchorCatalogItem.Create(
                        cityId: CityId.From(cityId),
                        cityAnchorId: CityAnchorId.From(anchorId),
                        districtId: DistrictId.From(districtId),
                        accessRoadNodeId: RoadNodeId.From(Guid.Parse("12121212-3434-5656-7878-909090909090")),
                        name: "Foundry",
                        type: CityAnchorType.Workplace,
                        capacity: 20,
                        positionX: 10m,
                        positionY: 20m,
                        createdAtUtc: UtcNow)
                ]
            };
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var personWriteRepository = new FakePersonWriteRepository();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            HireCityResidentCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cityPopulationAnchorCatalogRepository: anchorCatalogRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                cityEconomySettlementOutboxWriter: outboxWriter,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            CityEmploymentOperationResultDto result = await handler.Handle(
                request: new HireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    JobTitle: " Architect ",
                    WorkplaceId: null,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "EmploymentAssigned",
                actual: result.Action);
            Assert.Equal(
                expected: EmploymentStatus.Employed,
                actual: resident.Employment.Status);
            Assert.NotNull(resident.Employment.Job);
            Assert.Equal(
                expected: "Architect",
                actual: resident.Employment.Job!.Title);
            Assert.Equal(
                expected: CityAnchorId.From(anchorId),
                actual: resident.Employment.Job.WorkplaceAnchorId);
            Assert.Single(outboxWriter.WorkplaceBatches);
            Assert.NotNull(result.Resident.CurrentWorkplace);
            Assert.Equal(
                expected: "Architect",
                actual: result.Resident.JobTitle);
            Assert.Equal(
                expected: anchorId,
                actual: result.Resident.CurrentWorkplace.WorkplaceAnchorId);
        }

        [Fact]
        public async Task Handle_WhenWorkplaceIsMissing_ThrowsApplicationError()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var workplaceId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            Person resident = CreatePerson(
                personId: residentId,
                employmentStatus: EmploymentStatus.Unemployed);
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[resident.Id] = resident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
            var personWriteRepository = new FakePersonWriteRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var outboxWriter = new FakeCityEconomySettlementOutboxWriter();
            var unitOfWork = new FakeUnitOfWork();
            HireCityResidentCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                cityEconomySettlementOutboxWriter: outboxWriter,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(async ()
                => await handler.Handle(
                    request: new HireCityResidentCommand(
                        CityId: cityId,
                        ResidentId: residentId,
                        JobTitle: "Engineer",
                        WorkplaceId: workplaceId,
                        CurrentDate: new DateOnly(
                            year: 2048,
                            month: 5,
                            day: 5)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Employment.Workplace.NotFound",
                actual: exception.Code);
            Assert.Empty(personWriteRepository.UpdatedPersons);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Empty(outboxWriter.WorkplaceBatches);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static HireCityResidentCommandHandler CreateHandler(
            FakePersonReadRepository? personReadRepository = null,
            FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
            FakeCityPopulationAnchorCatalogRepository? cityPopulationAnchorCatalogRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityEconomySettlementOutboxWriter? cityEconomySettlementOutboxWriter = null,
            FakePersonWriteRepository? personWriteRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new HireCityResidentCommandHandler(
                personReadRepository: personReadRepository ?? new FakePersonReadRepository(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                cityPopulationAnchorCatalogRepository: cityPopulationAnchorCatalogRepository ??
                                                       new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                cityEconomySettlementOutboxWriter: cityEconomySettlementOutboxWriter ??
                                                   new FakeCityEconomySettlementOutboxWriter(),
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }
    }
}
