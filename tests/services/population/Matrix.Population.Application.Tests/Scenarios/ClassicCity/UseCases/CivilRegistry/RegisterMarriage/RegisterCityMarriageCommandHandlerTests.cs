using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage
{
    public sealed class RegisterCityMarriageCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenResidentsAreInDifferentHouseholds_MergesIntoHousedHousehold()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            var secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
            Person firstResident = CreatePerson(
                personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                householdId: firstHouseholdId.Value,
                firstName: "Neo",
                lastName: "Anderson",
                sex: Sex.Male);
            Person secondResident = CreatePerson(
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: secondHouseholdId.Value,
                firstName: "Trinity",
                lastName: "Unknown",
                sex: Sex.Female);
            var personReadRepository = new FakePersonReadRepository();
            personReadRepository.PersonsById[firstResident.Id] = firstResident;
            personReadRepository.PersonsById[secondResident.Id] = secondResident;
            var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
            cityPopulationPersonReadRepository.CityIdByPersonIds[firstResident.Id] = CityId.From(cityId);
            cityPopulationPersonReadRepository.CityIdByPersonIds[secondResident.Id] = CityId.From(cityId);
            var personWriteRepository = new FakePersonWriteRepository();
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            householdWriteRepository.HouseholdsById[firstHouseholdId] = CreateHousehold(
                householdId: firstHouseholdId,
                size: 1);
            householdWriteRepository.HouseholdsById[secondHouseholdId] = CreateHousehold(
                householdId: secondHouseholdId,
                size: 1);
            householdWriteRepository.PlacementsByHouseholdId[firstHouseholdId] =
                ClassicCityHouseholdPlacement.CreateHoused(
                    householdId: firstHouseholdId,
                    cityId: CityId.From(cityId),
                    districtId: DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
                    residentialBuildingId: ResidentialBuildingId.From(
                        Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")));
            householdWriteRepository.PlacementsByHouseholdId[secondHouseholdId] =
                ClassicCityHouseholdPlacement.CreateHomeless(
                    householdId: secondHouseholdId,
                    cityId: CityId.From(cityId));
            householdWriteRepository.ResidentCountByHouseholdId[firstHouseholdId] = 1;
            householdWriteRepository.ResidentCountByHouseholdId[secondHouseholdId] = 1;
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var educationProjectionRepository = new FakeEducationParticipationProjectionRepository();
            await educationProjectionRepository.UpsertNewerAsync(
                projections:
                [
                    new EducationParticipationProjection(
                        SimulationHostId: cityId,
                        ResidentId: firstResident.Id.Value,
                        ParticipationRevision: 1,
                        ResidentLifecycleRevision: firstResident.LifecycleRevision,
                        IsEnrolled: false,
                        ActiveStage: null,
                        InstitutionId: null,
                        InstitutionAnchorId: null,
                        EnrolledOn: null,
                        CompletedStage: "higher-education",
                        CompletedStageOn: new DateOnly(2045, 6, 20),
                        SnapshotDate: new DateOnly(2048, 5, 4),
                        OccurredAtUtc: UtcNow,
                        UpdatedAtUtc: UtcNow)
                ]);
            var unitOfWork = new FakeUnitOfWork();
            RegisterCityMarriageCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                educationProjectionRepository: educationProjectionRepository,
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
                expected: "MarriageRegistered",
                actual: result.Action);
            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: firstResident.MaritalStatus);
            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: secondResident.MaritalStatus);
            Assert.Equal(
                expected: firstResident.Id,
                actual: secondResident.SpouseId);
            Assert.Equal(
                expected: secondResident.Id,
                actual: firstResident.SpouseId);
            Assert.Equal(
                expected: firstHouseholdId,
                actual: secondResident.HouseholdId);
            Household updatedTargetHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
            Assert.Equal(
                expected: firstHouseholdId,
                actual: updatedTargetHousehold.Id);
            Assert.Equal(
                expected: 2,
                actual: updatedTargetHousehold.Size.Value);
            Household deletedSourceHousehold = Assert.Single(householdWriteRepository.DeletedHouseholds);
            Assert.Equal(
                expected: secondHouseholdId,
                actual: deletedSourceHousehold.Id);
            Assert.Equal(
                expected: 2,
                actual: personWriteRepository.UpdatedPersons.Count);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: firstResident.Id.Value,
                actual: result.FirstResident.Id);
            Assert.Equal(
                expected: "higher-education",
                actual: result.FirstResident.EducationLevel);
            Assert.NotNull(result.FirstResident.CurrentSpouse);
            Assert.Equal(
                expected: secondResident.Id.Value,
                actual: result.FirstResident.CurrentSpouse!.Id);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenResidentsAlreadyShareHousehold_DoesNotMutateHouseholds()
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
            RegisterCityMarriageCommandHandler handler = CreateHandler(
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
                expected: "MarriageRegistered",
                actual: result.Action);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.DeletedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
            Assert.Equal(
                expected: sharedHouseholdId,
                actual: firstResident.HouseholdId);
            Assert.Equal(
                expected: sharedHouseholdId,
                actual: secondResident.HouseholdId);
            Assert.Equal(
                expected: 2,
                actual: personWriteRepository.UpdatedPersons.Count);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static RegisterCityMarriageCommandHandler CreateHandler(
            FakePersonReadRepository? personReadRepository = null,
            FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeEducationParticipationProjectionRepository? educationProjectionRepository = null,
            FakePersonWriteRepository? personWriteRepository = null,
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new RegisterCityMarriageCommandHandler(
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

        private static RegisterCityMarriageCommand CreateCommand(
            Guid cityId,
            Guid firstResidentId,
            Guid secondResidentId)
        {
            return new RegisterCityMarriageCommand(
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
