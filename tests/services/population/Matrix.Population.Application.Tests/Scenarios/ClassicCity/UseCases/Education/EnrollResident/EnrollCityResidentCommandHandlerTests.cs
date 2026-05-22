using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.EnrollResident;

public sealed class EnrollCityResidentCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenInstitutionIsProvided_EnrollsResidentAndRebuildsSummary()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Guid institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        Guid institutionAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
        Person resident = CreatePerson(
            personId: residentId,
            employmentStatus: EmploymentStatus.Unemployed,
            happiness: 44);
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[resident.Id] = resident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository
        {
            EducationInstitutions =
            [
                new CityEducationInstitutionSnapshot(
                    InstitutionId: EducationInstitutionId.From(institutionId),
                    InstitutionAnchorId: CityAnchorId.From(institutionAnchorId),
                    EducationLevel: resident.EducationLevel,
                    ResidentCount: 48)
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
            new EnrollCityResidentCommand(
                CityId: cityId,
                ResidentId: residentId,
                InstitutionId: institutionId,
                CurrentDate: new DateOnly(2048, 5, 4)),
            CancellationToken.None);

        Assert.Equal("ResidentEnrolledInStudy", result.Action);
        Assert.Equal(EmploymentStatus.Student, resident.Employment.Status);
        Assert.Equal(EducationInstitutionId.From(institutionId), resident.Education.CurrentInstitutionId);
        Assert.Equal(CityAnchorId.From(institutionAnchorId), resident.Education.CurrentInstitutionAnchorId);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal(residentId, result.Resident.Id);
        Assert.Equal("Student", result.Resident.EmploymentStatus);
        Assert.NotNull(result.Resident.CurrentEducationInstitution);
        Assert.Equal(institutionId, result.Resident.CurrentEducationInstitution!.InstitutionId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static EnrollCityResidentCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new EnrollCityResidentCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            new FakeCityPopulationAnchorCatalogRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            new Matrix.Population.Domain.Scenarios.ClassicCity.Services.CityPopulationAnchorSelectionPolicy(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
