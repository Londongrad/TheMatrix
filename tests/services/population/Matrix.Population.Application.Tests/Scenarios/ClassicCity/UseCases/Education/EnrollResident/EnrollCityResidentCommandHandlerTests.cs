using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.EnrollResident
{
    public sealed class EnrollCityResidentCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenInstitutionIsProvided_EnrollsResidentAndRebuildsSummary()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            var institutionId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
            var institutionAnchorId = Guid.Parse("bbbbbbbb-1111-2222-3333-cccccccccccc");
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
            EnrollCityResidentCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            CityEducationOperationResultDto result = await handler.Handle(
                request: new EnrollCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    InstitutionId: institutionId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 4)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ResidentEnrolledInStudy",
                actual: result.Action);
            Assert.Equal(
                expected: EmploymentStatus.Student,
                actual: resident.Employment.Status);
            Assert.Equal(
                expected: EducationInstitutionId.From(institutionId),
                actual: resident.Education.CurrentInstitutionId);
            Assert.Equal(
                expected: CityAnchorId.From(institutionAnchorId),
                actual: resident.Education.CurrentInstitutionAnchorId);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: residentId,
                actual: result.Resident.Id);
            Assert.Equal(
                expected: "Student",
                actual: result.Resident.EmploymentStatus);
            Assert.NotNull(result.Resident.CurrentEducationInstitution);
            Assert.Equal(
                expected: institutionId,
                actual: result.Resident.CurrentEducationInstitution!.InstitutionId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
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
                personReadRepository: personReadRepository ?? new FakePersonReadRepository(),
                cityPopulationAnchorCatalogRepository: new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                anchorSelectionPolicy: new CityPopulationAnchorSelectionPolicy(),
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }
    }
}
