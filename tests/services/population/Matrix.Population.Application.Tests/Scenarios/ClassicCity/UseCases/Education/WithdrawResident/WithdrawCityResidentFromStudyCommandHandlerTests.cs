using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;

public sealed class WithdrawCityResidentFromStudyCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentIsStudent_StopsStudyAndMarksUnemployed()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed,
            happiness: 49);
        resident.StartStudying(
            currentDate: new DateOnly(2048, 5, 3),
            institutionId: EducationInstitutionId.From(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa")));
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

        var result = await handler.Handle(
            new WithdrawCityResidentFromStudyCommand(
                CityId: cityId,
                ResidentId: residentId,
                CurrentDate: new DateOnly(2048, 5, 4)),
            CancellationToken.None);

        Assert.Equal("ResidentWithdrawnFromStudy", result.Action);
        Assert.Equal(EmploymentStatus.Unemployed, resident.Employment.Status);
        Assert.Null(resident.Education.CurrentInstitutionId);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal("Unemployed", result.Resident.EmploymentStatus);
        Assert.Null(result.Resident.CurrentEducationInstitution);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenResidentIsNotStudent_ThrowsBusinessRule()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
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
                new WithdrawCityResidentFromStudyCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    CurrentDate: new DateOnly(2048, 5, 4)),
                CancellationToken.None));

        Assert.Equal("Population.Education.ResidentMustBeStudent", exception.Code);
        Assert.Empty(personWriteRepository.UpdatedPersons);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static WithdrawCityResidentFromStudyCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new WithdrawCityResidentFromStudyCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
