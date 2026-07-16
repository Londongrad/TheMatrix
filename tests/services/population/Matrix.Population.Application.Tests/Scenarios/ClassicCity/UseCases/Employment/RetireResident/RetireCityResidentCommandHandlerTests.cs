using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Employment.RetireResident
{
    public sealed class RetireCityResidentCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentIsSenior_RetiresResident()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            Person resident = CreatePerson(
                personId: residentId,
                birthDate: new DateOnly(
                    year: 1960,
                    month: 5,
                    day: 3),
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
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cityPopulationActivityJournalService: activityJournalService,
                cityPopulationSummaryProjectionService: summaryProjectionService,
                educationParticipationProjectionRepository:
                    new FakeEducationParticipationProjectionRepository(),
                personWriteRepository: personWriteRepository,
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork);

            CityEmploymentOperationResultDto result = await handler.Handle(
                request: new RetireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "ResidentRetired",
                actual: result.Action);
            Assert.Equal(
                expected: EmploymentStatus.Retired,
                actual: resident.Employment.Status);
            Assert.Null(resident.Employment.Job);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: "Retired",
                actual: result.Resident.EmploymentStatus);
            Assert.Null(result.Resident.CurrentWorkplace);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenResidentIsNotSenior_ThrowsDomainRule()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var residentId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            Person resident = CreatePerson(
                personId: residentId,
                birthDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 3),
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
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                cityPopulationActivityJournalService: activityJournalService,
                cityPopulationSummaryProjectionService: summaryProjectionService,
                educationParticipationProjectionRepository:
                    new FakeEducationParticipationProjectionRepository(),
                personWriteRepository: personWriteRepository,
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork);

            DomainException exception = await Assert.ThrowsAsync<DomainException>(async () => await handler.Handle(
                request: new RetireCityResidentCommand(
                    CityId: cityId,
                    ResidentId: residentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5)),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Person.Employment.OnlySeniorsCanRetire",
                actual: exception.Code);
            Assert.Empty(personWriteRepository.UpdatedPersons);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }
    }
}
