using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class CivilRegistryAutonomyStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly PreviousDate = new(
            year: 2047,
            month: 11,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 1);

        private static readonly DateTimeOffset OccurredAtUtc = new(
            year: 2048,
            month: 5,
            day: 1,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task ApplyAsync_WhenPolicyPlansNoDecisions_ReturnsZeroAndDoesNotMutate()
        {
            PersonEntity first = CreateAdultResident(
                personId: CreateGuid(1),
                householdId: CreateGuid(101));
            PersonEntity second = CreateAdultResident(
                personId: CreateGuid(2),
                householdId: CreateGuid(102));
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await CivilRegistryAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(
                    first,
                    second),
                previousDate: CurrentDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                marriageDomainService: new MarriageDomainService(),
                civilRegistryAutonomyPolicy: new CityCivilRegistryAutonomyPolicy(),
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: affected);
            Assert.Empty(activityEntries);
            Assert.Empty(householdWriteRepository.UpdatedHouseholds);
            Assert.Empty(householdWriteRepository.DeletedHouseholds);
            Assert.Empty(householdWriteRepository.AddedHouseholds);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: first.MaritalStatus);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: second.MaritalStatus);
        }

        [Fact]
        public async Task ApplyAsync_WhenMarriageDecisionIsPlanned_RegistersMarriageMergesHouseholdsAndWritesActivity()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            (PersonEntity[] residents, PersonEntity first, PersonEntity second) = FindStableMarriagePair(policy);
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            ConfigureHousedHousehold(
                repository: householdWriteRepository,
                householdId: first.HouseholdId,
                residentCount: 1);
            ConfigureHomelessHousehold(
                repository: householdWriteRepository,
                householdId: second.HouseholdId,
                residentCount: 1);
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await CivilRegistryAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(residents),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                marriageDomainService: new MarriageDomainService(),
                civilRegistryAutonomyPolicy: policy,
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: affected);
            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: first.MaritalStatus);
            Assert.Equal(
                expected: MaritalStatus.Married,
                actual: second.MaritalStatus);
            Assert.Equal(
                expected: second.Id,
                actual: first.SpouseId);
            Assert.Equal(
                expected: first.Id,
                actual: second.SpouseId);
            Assert.Equal(
                expected: first.HouseholdId,
                actual: second.HouseholdId);
            HouseholdEntity updatedTargetHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
            Assert.Equal(
                expected: first.HouseholdId,
                actual: updatedTargetHousehold.Id);
            Assert.Equal(
                expected: 2,
                actual: updatedTargetHousehold.Size.Value);
            HouseholdEntity deletedSourceHousehold = Assert.Single(householdWriteRepository.DeletedHouseholds);
            Assert.Equal(
                expected: second.HouseholdId,
                actual: first.HouseholdId);
            Assert.Equal(
                expected: 1,
                actual: deletedSourceHousehold.Size.Value);

            CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.ResidentsMarried,
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
                expected: first.Id.Value,
                actual: activity.PrimaryResidentId);
            Assert.Equal(
                expected: second.Id.Value,
                actual: activity.SecondaryResidentId);
        }

        [Fact]
        public async Task ApplyAsync_WhenDivorceDecisionIsPlanned_RegistersDivorceSeparatesHouseholdsAndWritesActivity()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            (PersonEntity first, PersonEntity second, HouseholdId householdId) = FindStableDivorcePair(policy);
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            ConfigureHousedHousehold(
                repository: householdWriteRepository,
                householdId: householdId,
                residentCount: 2);
            List<CityPopulationActivityWriteModel> activityEntries = [];

            int affected = await CivilRegistryAutonomyStep.ApplyAsync(
                cityId: TestCityId,
                residentsById: CreateResidentsById(
                    first,
                    second),
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                householdWriteRepository: householdWriteRepository,
                marriageDomainService: new MarriageDomainService(),
                civilRegistryAutonomyPolicy: policy,
                activityEntries: activityEntries,
                occurredAtUtc: OccurredAtUtc,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: affected);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: first.MaritalStatus);
            Assert.Equal(
                expected: MaritalStatus.Single,
                actual: second.MaritalStatus);
            Assert.Null(first.SpouseId);
            Assert.Null(second.SpouseId);
            Assert.NotEqual(
                expected: householdId,
                actual: second.HouseholdId);
            HouseholdEntity updatedSharedHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
            Assert.Equal(
                expected: householdId,
                actual: updatedSharedHousehold.Id);
            Assert.Equal(
                expected: 1,
                actual: updatedSharedHousehold.Size.Value);
            (HouseholdEntity newHousehold, ClassicCityHouseholdPlacement newPlacement) =
                Assert.Single(householdWriteRepository.AddedHouseholds);
            Assert.Equal(
                expected: second.HouseholdId,
                actual: newHousehold.Id);
            Assert.Equal(
                expected: 1,
                actual: newHousehold.Size.Value);
            Assert.Equal(
                expected: OccurredAtUtc,
                actual: newHousehold.CreatedAtUtc);
            Assert.Equal(
                expected: HousingStatus.Housed,
                actual: newPlacement.HousingStatus);
            Assert.Equal(
                expected: CreateDistrictId(householdId),
                actual: newPlacement.DistrictId);
            Assert.Equal(
                expected: CreateResidentialBuildingId(householdId),
                actual: newPlacement.ResidentialBuildingId);

            CityPopulationActivityWriteModel activity = Assert.Single(activityEntries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.ResidentsDivorced,
                actual: activity.EventType);
            Assert.Equal(
                expected: CityPopulationActivitySource.Autonomy,
                actual: activity.Source);
            Assert.Equal(
                expected: CityPopulationActivitySeverity.Warning,
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
                expected: first.Id.Value,
                actual: activity.PrimaryResidentId);
            Assert.Equal(
                expected: second.Id.Value,
                actual: activity.SecondaryResidentId);
        }

        private static (PersonEntity[] Residents, PersonEntity First, PersonEntity Second) FindStableMarriagePair(
            CityCivilRegistryAutonomyPolicy policy)
        {
            for (int seed = 1; seed <= 500; seed++)
            {
                PersonEntity[] residents = Enumerable.Range(
                        start: 0,
                        count: 10)
                   .Select(offset => CreateAdultResident(
                        personId: CreateGuid((seed * 100) + offset + 1),
                        householdId: CreateGuid((seed * 100) + offset + 10_001),
                        optimism: 100,
                        discipline: 100,
                        sociability: 100,
                        riskTolerance: 50,
                        happiness: 100,
                        health: 100,
                        stress: 0,
                        socialNeed: 100,
                        birthDate: new DateOnly(
                            year: 2022,
                            month: 5,
                            day: 1).AddYears(-(offset % 3))))
                   .ToArray();

                IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = policy.Plan(
                    residents: residents,
                    previousDate: PreviousDate,
                    currentDate: CurrentDate);

                if (decisions.Count != 1 || decisions[0].Type != CityCivilRegistryAutonomyDecisionType.Marriage)
                    continue;

                CityCivilRegistryAutonomyDecision decision = decisions[0];
                PersonEntity first = residents.Single(x => x.Id == decision.FirstResidentId);
                PersonEntity second = residents.Single(x => x.Id == decision.SecondResidentId);
                return (residents, first, second);
            }

            throw new XunitException("Expected deterministic compatible residents to schedule one marriage.");
        }

        private static (PersonEntity First, PersonEntity Second, HouseholdId HouseholdId) FindStableDivorcePair(
            CityCivilRegistryAutonomyPolicy policy)
        {
            for (int seed = 1; seed <= 2_000; seed++)
            {
                var firstId = PersonId.From(CreateGuid(40_000 + seed));
                var secondId = PersonId.From(CreateGuid(50_000 + seed));
                var householdId = HouseholdId.From(CreateGuid(60_000 + seed));
                PersonEntity first = CreateAdultResident(
                    personId: firstId.Value,
                    householdId: householdId.Value,
                    maritalStatus: MaritalStatus.Married,
                    spouseId: secondId,
                    optimism: 0,
                    discipline: 50,
                    sociability: 50,
                    riskTolerance: 50,
                    happiness: 0,
                    health: 25,
                    stress: 100,
                    socialNeed: 100,
                    birthDate: new DateOnly(
                        year: 2018,
                        month: 5,
                        day: 1));
                PersonEntity second = CreateAdultResident(
                    personId: secondId.Value,
                    householdId: householdId.Value,
                    sex: Sex.Female,
                    maritalStatus: MaritalStatus.Married,
                    spouseId: firstId,
                    optimism: 0,
                    discipline: 50,
                    sociability: 50,
                    riskTolerance: 50,
                    happiness: 0,
                    health: 25,
                    stress: 100,
                    socialNeed: 100,
                    birthDate: new DateOnly(
                        year: 2019,
                        month: 5,
                        day: 1));

                IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = policy.Plan(
                    residents:
                    [
                        first,
                        second
                    ],
                    previousDate: PreviousDate,
                    currentDate: CurrentDate);

                if (decisions.Count == 1 && decisions[0].Type == CityCivilRegistryAutonomyDecisionType.Divorce)
                    return (first, second, householdId);
            }

            throw new XunitException("Expected deterministic married pair to schedule divorce.");
        }

        private static Dictionary<PersonId, PersonEntity> CreateResidentsById(params PersonEntity[] residents)
        {
            return residents.ToDictionary(x => x.Id);
        }

        private static PersonEntity CreateAdultResident(
            Guid personId,
            Guid householdId,
            Sex sex = Sex.Male,
            MaritalStatus maritalStatus = MaritalStatus.Single,
            PersonId? spouseId = null,
            DateOnly? birthDate = null,
            int optimism = 70,
            int discipline = 70,
            int sociability = 70,
            int riskTolerance = 50,
            int happiness = 70,
            int health = 80,
            int stress = 20,
            int socialNeed = 50)
        {
            return PersonEntity.CreatePerson(
                id: PersonId.From(personId),
                householdId: HouseholdId.From(householdId),
                name: new PersonName(
                    firstName: "Alex",
                    lastName: "Smirnov"),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
                educationLevel: EducationLevel.UpperSecondary,
                educationInstitutionId: null,
                educationInstitutionAnchorId: null,
                employmentStatus: EmploymentStatus.Unemployed,
                happinessLevel: HappinessLevel.From(happiness),
                energyLevel: EnergyLevel.From(70),
                stressLevel: StressLevel.From(stress),
                socialNeedLevel: SocialNeedLevel.From(socialNeed),
                personality: Personality.Create(
                    optimism: optimism,
                    discipline: discipline,
                    riskTolerance: riskTolerance,
                    sociability: sociability),
                birthDate: birthDate ??
                new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 1),
                healthLevel: HealthLevel.From(health),
                weight: BodyWeight.FromKilograms(70m),
                job: null,
                currentDate: CurrentDate,
                illness: IllnessInfo.Healthy());
        }

        private static void ConfigureHousedHousehold(
            FakeHouseholdWriteRepository repository,
            HouseholdId householdId,
            int residentCount)
        {
            repository.HouseholdsById[householdId] = CreateHousehold(
                householdId: householdId,
                size: residentCount);
            repository.PlacementsByHouseholdId[householdId] = ClassicCityHouseholdPlacement.CreateHoused(
                householdId: householdId,
                cityId: TestCityId,
                districtId: CreateDistrictId(householdId),
                residentialBuildingId: CreateResidentialBuildingId(householdId));
            repository.ResidentCountByHouseholdId[householdId] = residentCount;
        }

        private static void ConfigureHomelessHousehold(
            FakeHouseholdWriteRepository repository,
            HouseholdId householdId,
            int residentCount)
        {
            repository.HouseholdsById[householdId] = CreateHousehold(
                householdId: householdId,
                size: residentCount);
            repository.PlacementsByHouseholdId[householdId] = ClassicCityHouseholdPlacement.CreateHomeless(
                householdId: householdId,
                cityId: TestCityId);
            repository.ResidentCountByHouseholdId[householdId] = residentCount;
        }

        private static HouseholdEntity CreateHousehold(
            HouseholdId householdId,
            int size)
        {
            return HouseholdEntity.Create(
                id: householdId,
                size: HouseholdSize.From(size),
                createdAtUtc: UtcNow);
        }

        private static DistrictId CreateDistrictId(HouseholdId householdId)
        {
            return DistrictId.From(
                Guid.Parse(
                    $"11111111-1111-1111-1111-{Math.Abs(householdId.Value.GetHashCode()) % 1_000_000_000_000:000000000000}"));
        }

        private static ResidentialBuildingId CreateResidentialBuildingId(HouseholdId householdId)
        {
            return ResidentialBuildingId.From(
                Guid.Parse(
                    $"22222222-2222-2222-2222-{Math.Abs(householdId.Value.GetHashCode()) % 1_000_000_000_000:000000000000}"));
        }

        private static Guid CreateGuid(int seed)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
        }
    }
}
