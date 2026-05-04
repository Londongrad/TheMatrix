using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.HireResident;

public sealed class HireCityResidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenWorkplaceExists_AssignsEmploymentAndPublishesSyncBatch()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid workplaceId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        Guid workplaceAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
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
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            cityEconomySettlementOutboxWriter: outboxWriter,
            personWriteRepository: personWriteRepository,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new HireCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                JobTitle: "Unused",
                WorkplaceId: workplaceId,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal("EmploymentAssigned", result.Action);
        Assert.Equal(EmploymentStatus.Employed, resident.Employment.Status);
        Assert.NotNull(resident.Employment.Job);
        Assert.Equal(WorkplaceId.From(workplaceId), resident.Employment.Job!.WorkplaceId);
        Assert.Equal("Engineer", resident.Employment.Job.Title);
        Assert.Equal(CityAnchorId.From(workplaceAnchorId), resident.Employment.Job.WorkplaceAnchorId);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        var batch = Assert.Single(outboxWriter.WorkplaceBatches);
        var workplace = Assert.Single(batch.Workplaces);
        Assert.Equal(workplaceId, workplace.WorkplaceId);
        Assert.Equal("Engineer", workplace.JobTitle);
        Assert.Equal(1, workplace.ActiveWorkerCount);
        Assert.Equal("Employed", result.Resident.EmploymentStatus);
        Assert.NotNull(result.Resident.CurrentWorkplace);
        Assert.Equal(workplaceId, result.Resident.CurrentWorkplace!.WorkplaceId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenOnlyJobTitleIsProvided_UsesAnchorCatalogSelection()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid districtId = Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb");
        Guid anchorId = Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd");
        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed,
            happiness: 41);
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
        cityPopulationPersonReadRepository.HousingSnapshotsByPersonId[resident.Id] = new CityResidentHousingSnapshot(
            HouseholdId: resident.HouseholdId,
            HousingStatus: HousingStatus.Housed,
            DistrictId: DistrictId.From(districtId),
            ResidentialBuildingId: ResidentialBuildingId.From(Guid.Parse("eeeeeeee-1111-2222-3333-ffffffffffff")));
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
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            cityPopulationAnchorCatalogRepository: anchorCatalogRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            cityEconomySettlementOutboxWriter: outboxWriter,
            personWriteRepository: personWriteRepository,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new HireCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                JobTitle: " Architect ",
                WorkplaceId: null,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal("EmploymentAssigned", result.Action);
        Assert.Equal(EmploymentStatus.Employed, resident.Employment.Status);
        Assert.NotNull(resident.Employment.Job);
        Assert.Equal("Architect", resident.Employment.Job!.Title);
        Assert.Equal(CityAnchorId.From(anchorId), resident.Employment.Job.WorkplaceAnchorId);
        Assert.Single(outboxWriter.WorkplaceBatches);
        Assert.NotNull(result.Resident.CurrentWorkplace);
        Assert.Equal("Architect", result.Resident.JobTitle);
        Assert.Equal(anchorId, result.Resident.CurrentWorkplace.WorkplaceAnchorId);
    }

    [Fact]
    public async Task Handle_WhenWorkplaceIsMissing_ThrowsApplicationError()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid workplaceId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
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
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            cityEconomySettlementOutboxWriter: outboxWriter,
            personWriteRepository: personWriteRepository,
            unitOfWork: unitOfWork);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            async () => await handler.Handle(
                new HireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    JobTitle: "Engineer",
                    WorkplaceId: workplaceId,
                    CurrentDate: new DateOnly(2048, 5, 5)),
                CancellationToken.None));

        Assert.Equal("Population.Employment.Workplace.NotFound", exception.Code);
        Assert.Empty(personWriteRepository.UpdatedPersons);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Empty(outboxWriter.WorkplaceBatches);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
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
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            cityPopulationAnchorCatalogRepository ?? new FakeCityPopulationAnchorCatalogRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            cityEconomySettlementOutboxWriter ?? new FakeCityEconomySettlementOutboxWriter(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            new CityPopulationAnchorSelectionPolicy(),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
