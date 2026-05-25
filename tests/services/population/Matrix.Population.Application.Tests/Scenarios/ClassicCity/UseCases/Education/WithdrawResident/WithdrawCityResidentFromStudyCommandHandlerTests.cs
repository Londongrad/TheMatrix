using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Education.WithdrawResident
{
    public sealed class WithdrawCityResidentFromStudyCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentIsStudent_StopsStudyAndMarksUnemployed()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            Person resident = CreatePerson(
                personId: residentId,
                employmentStatus: EmploymentStatus.Unemployed,
                happiness: 49);
            resident.StartStudying(
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                institutionId: EducationInstitutionId.From(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa")));
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[resident.Id] = resident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[resident.Id] = CityId.From(cityId);
            var personWriteRepository = new FakePersonWriteRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var unitOfWork = new FakeUnitOfWork();
            WithdrawCityResidentFromStudyCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            CityEducationOperationResultDto result = await handler.Handle(
                request: new WithdrawCityResidentFromStudyCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 4)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ResidentWithdrawnFromStudy",
                actual: result.Action);
            Assert.Equal(
                expected: EmploymentStatus.Unemployed,
                actual: resident.Employment.Status);
            Assert.Null(resident.Education.CurrentInstitutionId);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: "Unemployed",
                actual: result.Resident.EmploymentStatus);
            Assert.Null(result.Resident.CurrentEducationInstitution);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenResidentIsNotStudent_ThrowsBusinessRule()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
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
            WithdrawCityResidentFromStudyCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(async ()
                => await handler.Handle(
                    request: new WithdrawCityResidentFromStudyCommand(
                        CityId: cityId,
                        ResidentId: residentId,
                        CurrentDate: new DateOnly(
                            year: 2048,
                            month: 5,
                            day: 4)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Education.ResidentMustBeStudent",
                actual: exception.Code);
            Assert.Empty(personWriteRepository.UpdatedPersons);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
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
                personReadRepository: personReadRepository ?? new FakePersonReadRepository(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }
    }
}
