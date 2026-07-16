using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce
{
    public sealed class RegisterCityDivorceCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentsShareHousehold_SeparatesSpouses()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var sharedHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            Person firstResident = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                householdId: sharedHouseholdId.Value,
                firstName: "Neo",
                sex: Sex.Male);
            Person secondResident = CreatePerson(
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: sharedHouseholdId.Value,
                firstName: "Trinity",
                sex: Sex.Female);
            var marriageDomainService = new MarriageDomainService();
            marriageDomainService.RegisterMarriage(
                person: firstResident,
                spouse: secondResident,
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3));
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[firstResident.Id] = firstResident;
            personReadRepository.PersonsById[secondResident.Id] = secondResident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[firstResident.Id] = CityId.From(cityId);
            cityPopulationPersonReadRepository.CityIdByPersonIds[secondResident.Id] = CityId.From(cityId);
            var personWriteRepository = new FakePersonWriteRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            householdWriteRepository.HouseholdsById[sharedHouseholdId] = CreateHousehold(
                householdId: sharedHouseholdId,
                size: 2);
            householdWriteRepository.PlacementsByHouseholdId[sharedHouseholdId] =
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: sharedHouseholdId,
                    cityId: CityId.From(cityId),
                    districtId: DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
                    residentialBuildingId: ResidentialBuildingId.From(
                        Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")));
            householdWriteRepository.ResidentCountByHouseholdId[sharedHouseholdId] = 2;
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var unitOfWork = new FakeUnitOfWork();
            RegisterCityDivorceCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                unitOfWork: unitOfWork);

            CityCivilRegistryOperationResultDto result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    firstResidentId: firstResident.Id.Value,
                    secondResidentId: secondResident.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "DivorceRegistered",
                actual: result.Action);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: firstResident.MaritalStatus);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: secondResident.MaritalStatus);
            Assert.Null(firstResident.SpouseId);
            Assert.Null(secondResident.SpouseId);
            Assert.NotEqual(
                expected: sharedHouseholdId,
                actual: secondResident.HouseholdId);
            Household updatedSharedHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
            Assert.Equal(
                expected: sharedHouseholdId,
                actual: updatedSharedHousehold.Id);
            Assert.Equal(
                expected: 1,
                actual: updatedSharedHousehold.Size.Value);
            (Household Household, ClassicCityHouseholdPlacement Placement) addedHousehold =
                Assert.Single(householdWriteRepository.AddedHouseholds);
            Assert.Equal(
                expected: secondResident.HouseholdId,
                actual: addedHousehold.Household.Id);
            Assert.Equal(
                expected: HousingStatus.Housed,
                actual: addedHousehold.Placement.HousingStatus);
            Assert.Equal(
                expected: DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
                actual: addedHousehold.Placement.DistrictId);
            Assert.Equal(
                expected: 2,
                actual: personWriteRepository.UpdatedPersons.Count);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Null(result.FirstResident.CurrentSpouse);
            Assert.Null(result.SecondResident.CurrentSpouse);
            Assert.Equal(
                expected: secondResident.HouseholdId.Value,
                actual: result.SecondResident.CurrentHousing.HouseholdId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenResidentsAreNotCurrentSpouses_ThrowsBusinessRule()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
            Person firstResident = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                householdId: firstHouseholdId.Value,
                firstName: "Neo",
                sex: Sex.Male);
            Person secondResident = CreatePerson(
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: secondHouseholdId.Value,
                firstName: "Trinity",
                sex: Sex.Female);
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[firstResident.Id] = firstResident;
            personReadRepository.PersonsById[secondResident.Id] = secondResident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[firstResident.Id] = CityId.From(cityId);
            cityPopulationPersonReadRepository.CityIdByPersonIds[secondResident.Id] = CityId.From(cityId);
            var personWriteRepository = new FakePersonWriteRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var unitOfWork = new FakeUnitOfWork();
            RegisterCityDivorceCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository,
                householdWriteRepository: householdWriteRepository,
                unitOfWork: unitOfWork);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(async ()
                => await handler.Handle(
                    request: CreateCommand(
                        cityId: cityId,
                        firstResidentId: firstResident.Id.Value,
                        secondResidentId: secondResident.Id.Value),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.CivilRegistry.ResidentsAreNotCurrentSpouses",
                actual: exception.Code);
            Assert.Empty(personWriteRepository.UpdatedPersons);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
            Assert.Empty(activityJournalService.Entries);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static RegisterCityDivorceCommandHandler CreateHandler(
            FakePersonReadRepository? personReadRepository = null,
            FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeEducationParticipationProjectionRepository? educationProjectionRepository = null,
            FakePersonWriteRepository? personWriteRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new RegisterCityDivorceCommandHandler(
                personReadRepository: personReadRepository ?? new FakePersonReadRepository(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                educationParticipationProjectionRepository: educationProjectionRepository ??
                                                            new FakeEducationParticipationProjectionRepository(),
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                marriageDomainService: new MarriageDomainService(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static RegisterCityDivorceCommand CreateCommand(
            Guid cityId,
            Guid firstResidentId,
            Guid secondResidentId)
        {
            return new RegisterCityDivorceCommand(
                CityId: cityId,
                FirstResidentId: firstResidentId,
                SecondResidentId: secondResidentId,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 4));
        }

        private static Household CreateHousehold(
            HouseholdId householdId,
            int size)
        {
            return Household.Create(
                id: householdId,
                size: HouseholdSize.From(size),
                createdAtUtc: UtcNow);
        }
    }
}
