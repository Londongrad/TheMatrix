using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.FireResident;

public sealed class FireCityResidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentIsEmployed_FiresResidentAndClearsJob()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Person resident = CreatePerson(
            personId: residentId,
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
        var handler = new FireCityResidentCommandHandler(
            personReadRepository,
            cityPopulationPersonReadRepository,
            activityJournalService,
            summaryProjectionService,
            personWriteRepository,
            unitOfWork);

        var result = await handler.Handle(
            new FireCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                CurrentDate: new DateOnly(2048, 5, 5)),
            CancellationToken.None);

        Assert.Equal("ResidentFired", result.Action);
        Assert.Equal(EmploymentStatus.Unemployed, resident.Employment.Status);
        Assert.Null(resident.Employment.Job);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal("Unemployed", result.Resident.EmploymentStatus);
        Assert.Null(result.Resident.CurrentWorkplace);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
