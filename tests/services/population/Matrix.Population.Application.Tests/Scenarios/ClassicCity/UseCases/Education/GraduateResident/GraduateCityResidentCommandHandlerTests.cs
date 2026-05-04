using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.GraduateResident;

public sealed class GraduateCityResidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenTargetInstitutionMatches_GraduatesResident()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        Guid institutionAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed,
            happiness: 51);
        resident.StartStudying(
            currentDate: new DateOnly(2048, 5, 3),
            institutionId: EducationInstitutionId.From(Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee")));
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository
        {
            EducationInstitutions =
            [
                new CityEducationInstitutionSnapshot(
                    InstitutionId: EducationInstitutionId.From(institutionId),
                    InstitutionAnchorId: CityAnchorId.From(institutionAnchorId),
                    EducationLevel: EducationLevel.Higher,
                    ResidentCount: 120)
            ]
        };
        cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            personWriteRepository: personWriteRepository,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            new GraduateCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                TargetEducationLevel: "Higher",
                InstitutionId: institutionId,
                CurrentDate: new DateOnly(2048, 5, 4)),
            CancellationToken.None);

        Assert.Equal("ResidentGraduated", result.Action);
        Assert.Equal(EducationLevel.Higher, resident.EducationLevel);
        Assert.Equal(EducationInstitutionId.From(institutionId), resident.Education.CurrentInstitutionId);
        Assert.Equal(CityAnchorId.From(institutionAnchorId), resident.Education.CurrentInstitutionAnchorId);
        Assert.Equal(EmploymentStatus.Student, resident.Employment.Status);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal("Higher", result.Resident.EducationLevel);
        Assert.NotNull(result.Resident.CurrentEducationInstitution);
        Assert.Equal(institutionId, result.Resident.CurrentEducationInstitution!.InstitutionId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenTargetLevelIsInvalid_ThrowsValidationError()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed);
        resident.StartStudying(
            currentDate: new DateOnly(2048, 5, 3),
            institutionId: EducationInstitutionId.From(Guid.Parse("dddddddd-1111-2222-3333-eeeeeeeeeeee")));
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            personWriteRepository: personWriteRepository,
            unitOfWork: unitOfWork);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            async () => await handler.Handle(
                new GraduateCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    TargetEducationLevel: "NotALevel",
                    InstitutionId: null,
                    CurrentDate: new DateOnly(2048, 5, 4)),
                CancellationToken.None));

        Assert.Equal("Population.Education.Level.Invalid", exception.Code);
        Assert.Empty(personWriteRepository.UpdatedPersons);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static GraduateCityResidentCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new GraduateCityResidentCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            new Matrix.Population.Domain.Scenarios.ClassicCity.Services.CityPopulationAnchorSelectionPolicy(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
