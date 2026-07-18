using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityHouseholdIndependenceAutonomyPolicyTests
    {
        [Fact]
        public void Plan_WhenReviewWindowDoesNotAdvance_ReturnsEmpty()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            var householdId = HouseholdId.From(Guid.Parse("10101010-2020-3030-4040-505050505050"));

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    CreateResident(
                        personId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001"),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2010,
                            month: 5,
                            day: 3))
                ],
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3));

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenHouseholdIsNotHoused_ReturnsEmpty()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            var householdId = HouseholdId.From(Guid.Parse("20202020-3030-4040-5050-606060606060"));
            var motherId = PersonId.From(Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"));
            var fatherId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    CreateResident(
                        personId: motherId.Value,
                        householdId: householdId,
                        sex: Sex.Female,
                        birthDate: new DateOnly(
                            year: 2013,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: fatherId),
                    CreateResident(
                        personId: fatherId.Value,
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2011,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: motherId),
                    CreateResident(
                        personId: Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2024,
                            month: 5,
                            day: 3),
                        motherId: motherId,
                        fatherId: fatherId,
                        employmentStatus: EmploymentStatus.Employed)
                ],
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Homeless
                },
                previousDate: new DateOnly(
                    year: 2047,
                    month: 11,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3));

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenOnlyEligibleAdultHasChildInSameHousehold_ReturnsEmpty()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            var householdId = HouseholdId.From(Guid.Parse("30303030-4040-5050-6060-707070707070"));
            var candidateId = PersonId.From(Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444"));

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    CreateResident(
                        personId: candidateId.Value,
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2023,
                            month: 5,
                            day: 3),
                        employmentStatus: EmploymentStatus.Employed,
                        maritalStatus: MaritalStatus.Single,
                        happiness: 20,
                        stress: 90),
                    CreateResident(
                        personId: Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2048,
                            month: 3,
                            day: 1),
                        employmentStatus: EmploymentStatus.None,
                        motherId: candidateId,
                        sex: Sex.Female),
                    CreateResident(
                        personId: Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666"),
                        householdId: householdId,
                        birthDate: new DateOnly(
                            year: 2018,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: PersonId.From(Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777"))),
                    CreateResident(
                        personId: Guid.Parse("77777777-aaaa-bbbb-cccc-777777777777"),
                        householdId: householdId,
                        sex: Sex.Female,
                        birthDate: new DateOnly(
                            year: 2019,
                            month: 5,
                            day: 3),
                        maritalStatus: MaritalStatus.Married,
                        spouseId: PersonId.From(Guid.Parse("66666666-aaaa-bbbb-cccc-666666666666")))
                ],
                routineProfilesByResidentId: EmptyRoutineProfiles(),
                housingStatuses: new Dictionary<HouseholdId, HousingStatus>
                {
                    [householdId] = HousingStatus.Housed
                },
                previousDate: new DateOnly(
                    year: 2047,
                    month: 11,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3));

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenCrowdedAdultLivesWithParentsAndRollOccurs_ReturnsMoveOutDecision()
        {
            CityHouseholdIndependenceAutonomyPolicy policy = CreatePolicy();
            DateOnly previousDate = new(
                year: 2047,
                month: 11,
                day: 1);
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 3);

            for (int seed = 1; seed <= 1_000; seed++)
            {
                var householdId = HouseholdId.From(CreateGuid(100_000 + seed));
                var motherId = PersonId.From(CreateGuid(110_000 + seed));
                var fatherId = PersonId.From(CreateGuid(120_000 + seed));
                var candidateId = PersonId.From(CreateGuid(130_000 + seed));

                Person[] residents =
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
                    previousDate: previousDate,
                    currentDate: currentDate);

                CityHouseholdIndependenceAutonomyDecision? decision = decisions.SingleOrDefault();
                if (decision is not null)
                {
                    Assert.Equal(
                        expected: candidateId,
                        actual: decision.ResidentId);
                    Assert.Equal(
                        expected: householdId,
                        actual: decision.SourceHouseholdId);
                    return;
                }
            }

            throw new XunitException(
                "Expected at least one deterministic crowded household to produce a move-out decision.");
        }

        private static CityHouseholdIndependenceAutonomyPolicy CreatePolicy()
        {
            return new CityHouseholdIndependenceAutonomyPolicy(
                householdLivelihoodPolicy: new CityHouseholdLivelihoodPolicy());
        }

        private static IReadOnlyDictionary<PersonId, PersonRoutineProfile> EmptyRoutineProfiles()
        {
            return new Dictionary<PersonId, PersonRoutineProfile>();
        }

        private static Person CreateResident(
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
            return Person.CreatePerson(
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
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 3),
                motherId: motherId,
                fatherId: fatherId);
        }

        private static Guid CreateGuid(int seed)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
        }
    }
}
