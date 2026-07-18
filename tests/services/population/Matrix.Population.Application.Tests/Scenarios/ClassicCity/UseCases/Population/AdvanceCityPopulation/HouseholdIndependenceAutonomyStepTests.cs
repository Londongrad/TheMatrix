using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class HouseholdIndependenceAutonomyStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly PreviousDate = new(
            year: 2047,
            month: 11,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 3);

        private static readonly DateTimeOffset OccurredAtUtc = new(
            year: 2048,
            month: 5,
            day: 3,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task ApplyAsync_WhenNoPlacementsExist_ReturnsZeroAndDoesNotPlanMoves()
        {
            PersonEntity resident = CreateResident(
                personId: CreateGuid(1),
                householdId: HouseholdId.From(CreateGuid(101)),
                birthDate: new DateOnly(
                    year: 2023,
                    month: 5,
                    day: 3));
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                PlacementsByCityResult = []
            };
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(resident),
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                householdIndependenceAutonomyPolicy: CreatePolicy(),
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Equal(
                expected: TestCityId,
                actual: householdWriteRepository.RequestedCityId);
            Assert.Empty(activityEntries);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
        }

        [Fact]
        public async Task ApplyAsync_WhenPolicyPlansNoMoves_ReturnsZero()
        {
            var householdId = HouseholdId.From(CreateGuid(201));
            PersonEntity resident = CreateResident(
                personId: CreateGuid(2),
                householdId: householdId,
                birthDate: new DateOnly(
                    year: 2023,
                    month: 5,
                    day: 3));
            ClassicCityHouseholdPlacement placement = CreateHousedPlacement(householdId);
            var householdWriteRepository = new FakeHouseholdWriteRepository
            {
                PlacementsByCityResult = [placement]
            };
            householdWriteRepository.PlacementsByHouseholdId[householdId] = placement;
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(resident),
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                previousDate: CurrentDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                householdIndependenceAutonomyPolicy: CreatePolicy(),
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Equal(
                expected: householdId,
                actual: resident.HouseholdId);
            Assert.Empty(activityEntries);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
        }

        [Fact]
        public async Task
            ApplyAsync_WhenMoveOutDecisionIsPlanned_MovesResidentCreatesIndependentHouseholdAndWritesActivity()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            (PersonEntity[] residents, PersonEntity candidate, HouseholdId householdId) =
                FindStableMoveOutHousehold(policy);
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            ConfigureHousedHousehold(
                repository: householdWriteRepository,
                householdId: householdId,
                residentsCount: residents.Length,
                cashReserve: 10_000m);
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(residents),
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                householdIndependenceAutonomyPolicy: policy,
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: affected);
            Assert.NotEqual(
                expected: householdId,
                actual: candidate.HouseholdId);
            HouseholdEntity updatedSourceHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
            Assert.Equal(
                expected: householdId,
                actual: updatedSourceHousehold.Id);
            Assert.Equal(
                expected: residents.Length - 1,
                actual: updatedSourceHousehold.Size.Value);
            (HouseholdEntity newHousehold, ClassicCityHouseholdPlacement newPlacement) =
                Assert.Single(householdWriteRepository.AddedHouseholds);
            Assert.Equal(
                expected: candidate.HouseholdId,
                actual: newHousehold.Id);
            Assert.Equal(
                expected: 1,
                actual: newHousehold.Size.Value);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: newHousehold.CreatedAtUtc);
            Assert.Equal(
                expected: Money.FromDecimal(3_200m),
                actual: newHousehold.CashReserve);
            Assert.Equal(
                expected: candidate.HouseholdId,
                actual: newPlacement.HouseholdId);
            Assert.Equal(
                expected: TestCityId,
                actual: newPlacement.CityId);
            Assert.Equal(
                expected: HousingStatus.Homeless,
                actual: newPlacement.HousingStatus);

            CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.ResidentFormedIndependentHousehold,
                actual: activity.EventType);
            Assert.Equal(
                expected: CityPopulationActivitySource.Autonomy,
                actual: activity.Source);
            Assert.Equal(
                expected: CityPopulationActivitySeverity.Success,
                actual: activity.Severity);
            Assert.Equal(
                expected: TestCityId.Value,
                actual: activity.CityId);
            Assert.Equal(
                expected: CurrentDate,
                actual: activity.CurrentDate);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: activity.OccurredAtUtc);
            Assert.Equal(
                expected: candidate.Id.Value,
                actual: activity.PrimaryResidentId);
        }

        [Fact]
        public async Task ApplyAsync_WhenSourceHouseholdHasOneResident_SkipsMove()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            (PersonEntity[] residents, PersonEntity candidate, HouseholdId householdId) =
                FindStableMoveOutHousehold(policy);
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            ConfigureHousedHousehold(
                repository: householdWriteRepository,
                householdId: householdId,
                residentsCount: 1,
                cashReserve: 10_000m);
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await HouseholdIndependenceAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(residents),
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                householdIndependenceAutonomyPolicy: policy,
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Equal(
                expected: householdId,
                actual: candidate.HouseholdId);
            Assert.Empty(activityEntries);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
        }

        private static CityHouseholdIndependenceAutonomyPolicy CreatePolicy()
        {
            return new CityHouseholdIndependenceAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
        }

        private static (PersonEntity[] Residents, PersonEntity Candidate, HouseholdId HouseholdId)
            FindStableMoveOutHousehold(CityHouseholdIndependenceAutonomyPolicy policy)
        {
            for (int seed = 1; seed <= 1_000; seed++)
            {
                var householdId = HouseholdId.From(CreateGuid(100_000 + seed));
                var motherId = PersonId.From(CreateGuid(110_000 + seed));
                var fatherId = PersonId.From(CreateGuid(120_000 + seed));
                var candidateId = PersonId.From(CreateGuid(130_000 + seed));
                PersonEntity[] residents =
                [
                    CreateResident(
                        personId: motherId.Value,
                        householdId: householdId,
                        sex: Sex.Female,
                        birthDate: new DateOnly(
                            year: 2010,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: fatherId,
                        happiness: 20,
                        stress: 95),
                    CreateResident(
                        personId: fatherId.Value,
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2008,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: motherId,
                        happiness: 20,
                        stress: 95),
                    CreateResident(
                        personId: candidateId.Value,
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2023,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Single,
                        employmentStatus: EmploymentStatus.Employed,
                        happiness: 0,
                        health: 100,
                        stress: 100,
                        motherId: motherId,
                        fatherId: fatherId,
                        personality: Personality.Create(
                            optimism: 100,
                            discipline: 100,
                            riskTolerance: 50,
                            sociability: 80)),
                    CreateResident(
                        personId: CreateGuid(140_000 + seed),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2037,
                            month: 5,
                            day: 3),
                        employmentStatus: EmploymentStatus.None,
                        motherId: motherId,
                        fatherId: fatherId,
                        happiness: 30,
                        stress: 80),
                    CreateResident(
                        personId: CreateGuid(150_000 + seed),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2040,
                            month: 5,
                            day: 3),
                        employmentStatus: EmploymentStatus.None,
                        motherId: motherId,
                        fatherId: fatherId,
                        happiness: 30,
                        stress: 80)
                ];

                IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
                    residents: residents,
                    routineProfilesByResidentId: EmptyRoutineProfiles(),
                    housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                    {
                        [householdId] = HousingStatus.Housed
                    },
                    previousDate: PreviousDate,
                    currentDate: CurrentDate);

                CityHouseholdIndependenceAutonomyDecision? decision = decisions.SingleOrDefault();
                if (decision is null)
                    continue;

                PersonEntity candidate = residents.Single(x => x.Id == decision.ResidentId);
                return (residents, candidate, householdId);
            }

            throw new XunitException("Expected deterministic crowded household to produce a move-out decision.");
        }

        private static Dictionary<PersonId, PersonEntity> CreateResidentsById(params PersonEntity[] residents)
        {
            return residents.ToDictionary(x => x.Id);
        }

        private static IReadOnlyDictionary<PersonId, PersonRoutineProfile> EmptyRoutineProfiles()
        {
            return new Dictionary<PersonId, PersonRoutineProfile>();
        }

        private static PersonEntity CreateResident(
            Guid personId,
            HouseholdId householdId,
            DateOnly birthDate,
            Sex sex = Sex.Male,
            MaritalStatus maritalStatus = MaritalStatus.Single,
            PersonId? spouseId = null,
            EmploymentStatus employmentStatus = EmploymentStatus.Unemployed,
            int happiness = 50,
            int health = 80,
            int stress = 25,
            PersonId? motherId = null,
            PersonId? fatherId = null,
            Personality? personality = null)
        {
            return PersonEntity.CreatePerson(
                id: PersonId.From(personId),
                householdId: householdId,
                name: new PersonName(
                    firstName: "Alex",
                    lastName: "Petrov"),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
                employmentStatus: employmentStatus,
                happinessLevel: HappinessLevel.From(happiness),
                energyLevel: EnergyLevel.From(80),
                stressLevel: StressLevel.From(stress),
                socialNeedLevel: SocialNeedLevel.From(40),
                personality: personality ?? Personality.Neutral(),
                birthDate: birthDate,
                healthLevel: HealthLevel.From(health),
                weight: BodyWeight.FromKilograms(70m),
                job: employmentStatus == EmploymentStatus.Employed
                    ? new Job(
                        workplaceId: WorkplaceId.From(Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc")),
                        title: "Engineer",
                        workplaceAnchorId: null)
                    : null,
                currentDate: CurrentDate,
                motherId: motherId,
                fatherId: fatherId);
        }

        private static void ConfigureHousedHousehold(
            FakeHouseholdWriteRepository repository,
            HouseholdId householdId,
            int residentsCount,
            decimal cashReserve)
        {
            ClassicCityHouseholdPlacement placement = CreateHousedPlacement(householdId);
            repository.PlacementsByCityResult = [placement];
            repository.HouseholdsById[householdId] = CreateHousehold(
                householdId: householdId,
                size: residentsCount,
                cashReserve: cashReserve);
            repository.PlacementsByHouseholdId[householdId] = placement;
            repository.ResidentCountByHouseholdId[householdId] = residentsCount;
        }

        private static HouseholdEntity CreateHousehold(
            HouseholdId householdId,
            int size,
            decimal cashReserve)
        {
            return HouseholdEntity.Create(
                id: householdId,
                size: HouseholdSize.From(size),
                createdAtUtc: CreatedAtUtc,
                cashReserve: Money.FromDecimal(cashReserve));
        }

        private static ClassicCityHouseholdPlacement CreateHousedPlacement(HouseholdId householdId)
        {
            return ClassicCityHouseholdPlacement.CreateHoused(
                householdId: householdId,
                cityId: TestCityId,
                districtId: DistrictId.From(
                    CreateGuid((Math.Abs(householdId.Value.GetHashCode()) % 900_000) + 200_000)),
                residentialBuildingId: ResidentialBuildingId.From(
                    CreateGuid((Math.Abs(householdId.Value.GetHashCode()) % 900_000) + 300_000)));
        }

        private static Guid CreateGuid(int seed)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
        }
    }
}
