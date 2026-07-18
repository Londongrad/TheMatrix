using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using Xunit.Sdk;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityCivilRegistryAutonomyPolicyTests
    {
        [Fact]
        public void Plan_WhenReviewWindowDoesNotAdvance_ReturnsEmpty()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            Person first = CreateResident(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
            Person second = CreateResident(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"));

            IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    first,
                    second
                ],
                routineProfilesByResidentId: CreateRoutineProfiles(
                    first,
                    second),
                previousDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenCompatibleResidentsRollMarriage_ReturnsMarriageDecision()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            DateOnly previousDate = new(
                year: 2047,
                month: 11,
                day: 1);
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);

            for (int seed = 1; seed <= 500; seed++)
            {
                Person[] residents = Enumerable.Range(
                        start: 0,
                        count: 10)
                   .Select(offset => CreateResident(
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
                    routineProfilesByResidentId: CreateRoutineProfiles(residents),
                    previousDate: previousDate,
                    currentDate: currentDate);

                CityCivilRegistryAutonomyDecision? marriageDecision =
                    decisions.FirstOrDefault(x => x.Type == CityCivilRegistryAutonomyDecisionType.Marriage);
                if (marriageDecision is not null)
                {
                    Assert.Contains(
                        collection: residents,
                        filter: x => x.Id == marriageDecision.FirstResidentId);
                    Assert.Contains(
                        collection: residents,
                        filter: x => x.Id == marriageDecision.SecondResidentId);
                    Assert.NotEqual(
                        expected: marriageDecision.FirstResidentId,
                        actual: marriageDecision.SecondResidentId);
                    return;
                }
            }

            throw new XunitException("Expected at least one deterministic compatible pair to schedule marriage.");
        }

        [Fact]
        public void Plan_WhenResidentsShareSameHousehold_DoesNotScheduleMarriage()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            var sharedHouseholdId = HouseholdId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
            Person first = CreateResident(
                personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                householdId: sharedHouseholdId.Value,
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
                    day: 1));
            Person second = CreateResident(
                personId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                householdId: sharedHouseholdId.Value,
                optimism: 100,
                discipline: 100,
                sociability: 100,
                riskTolerance: 50,
                happiness: 100,
                health: 100,
                stress: 0,
                socialNeed: 100,
                birthDate: new DateOnly(
                    year: 2021,
                    month: 5,
                    day: 1));

            IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = policy.Plan(
                residents:
                [
                    first,
                    second
                ],
                routineProfilesByResidentId: CreateRoutineProfiles(
                    first,
                    second),
                previousDate: new DateOnly(
                    year: 2047,
                    month: 11,
                    day: 1),
                currentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));

            Assert.Empty(decisions);
        }

        [Fact]
        public void Plan_WhenMarriedResidentsRollDivorce_ReturnsDivorceDecision()
        {
            var policy = new CityCivilRegistryAutonomyPolicy();
            DateOnly previousDate = new(
                year: 2047,
                month: 11,
                day: 1);
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);

            for (int seed = 1; seed <= 2_000; seed++)
            {
                var firstId = PersonId.From(CreateGuid(40_000 + seed));
                var secondId = PersonId.From(CreateGuid(50_000 + seed));
                Guid householdId = CreateGuid(60_000 + seed);
                Person first = CreateResident(
                    personId: firstId.Value,
                    householdId: householdId,
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
                Person second = CreateResident(
                    personId: secondId.Value,
                    householdId: householdId,
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
                    routineProfilesByResidentId: CreateRoutineProfiles(
                        first,
                        second),
                    previousDate: previousDate,
                    currentDate: currentDate);

                if (decisions.Count == 1 && decisions[0].Type == CityCivilRegistryAutonomyDecisionType.Divorce)
                {
                    CityCivilRegistryAutonomyDecision decision = decisions[0];
                    Assert.Equal(
                        expected: first.Id,
                        actual: decision.FirstResidentId);
                    Assert.Equal(
                        expected: second.Id,
                        actual: decision.SecondResidentId);
                    return;
                }
            }

            throw new XunitException("Expected at least one deterministic married pair to schedule divorce.");
        }

        private static Person CreateResident(
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
            DateOnly currentDate = new(
                year: 2048,
                month: 5,
                day: 1);

            return Person.CreatePerson(
                id: PersonId.From(personId),
                householdId: HouseholdId.From(householdId),
                name: new PersonName(
                    firstName: "Alex",
                    lastName: "Smirnov"),
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
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
                currentDate: currentDate);
        }

        private static Dictionary<PersonId, PersonRoutineProfile> CreateRoutineProfiles(
            params Person[] residents)
        {
            return residents.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: _ => PersonRoutineProfile.Unstructured);
        }

        private static Guid CreateGuid(int seed)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{seed:000000000000}");
        }
    }
}
