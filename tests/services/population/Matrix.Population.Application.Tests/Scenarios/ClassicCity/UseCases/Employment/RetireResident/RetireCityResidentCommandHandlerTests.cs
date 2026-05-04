using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.RetireResident;

public sealed class RetireCityResidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentIsSenior_RetiresResident()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Person resident = CreatePerson(
            personId: residentId,
            birthDate: new DateOnly(1960, 5, 3),
            employmentStatus: EmploymentStatus.Unemployed);
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RetireCityResidentCommandHandler(
            personReadRepository,
            cityPopulationPersonReadRepository,
            activityJournalService,
            summaryProjectionService,
            personWriteRepository,
            unitOfWork);

        var result = await handler.Handle(
            new RetireCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal("ResidentRetired", result.Action);
        Assert.Equal(EmploymentStatus.Retired, resident.Employment.Status);
        Assert.Null(resident.Employment.Job);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal("Retired", result.Resident.EmploymentStatus);
        Assert.Null(result.Resident.CurrentWorkplace);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenResidentIsNotSenior_ThrowsDomainRule()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Person resident = CreatePerson(
            personId: residentId,
            birthDate: new DateOnly(2030, 5, 3),
            employmentStatus: EmploymentStatus.Employed,
            job: new Job(
                workplaceId: WorkplaceId.From(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa")),
                title: "Engineer"));
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RetireCityResidentCommandHandler(
            personReadRepository,
            cityPopulationPersonReadRepository,
            activityJournalService,
            summaryProjectionService,
            personWriteRepository,
            unitOfWork);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            async () => await handler.Handle(
                new RetireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    CurrentDate: new DateOnly(2048, 5, 5)),
                CancellationToken.None));

        Assert.Equal("Population.Person.Employment.OnlySeniorsCanRetire", exception.Code);
        Assert.Empty(personWriteRepository.UpdatedPersons);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }
}
