using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentNeedsProgressionStepTests
    {
        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 6);

        private static readonly DateTimeOffset FromUtc = new(
            year: 2048,
            month: 5,
            day: 6,
            hour: 8,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        private static readonly DateTimeOffset ToUtc = FromUtc.AddHours(4);

        [Fact]
        public void Apply_WhenToTimeIsNotAfterFromTime_ReturnsFalseAndDoesNotChangeNeeds()
        {
            Person resident = CreatePerson();
            var before = NeedsSnapshot.Capture(resident);

            ResidentProgressionStepResult result = Apply(
                resident: resident,
                fromUtc: FromUtc,
                toUtc: FromUtc);

            Assert.False(result.HasAnyEffect);
            Assert.Equal(
                expected: before,
                actual: NeedsSnapshot.Capture(resident));
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenTimeAdvances_AppliesCalculatedNeedsProgressionEffect()
        {
            Person resident = CreatePerson();
            var policy = new PersonNeedsProgressionPolicy();
            var before = NeedsSnapshot.Capture(resident);
            PersonNeedsProgressionEffect expectedEffect = policy.Calculate(
                person: resident,
                fromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                utcOffsetMinutes: 600);

            ResidentProgressionStepResult result = Apply(
                resident: resident,
                fromUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                environment: CreateEnvironment(utcOffsetMinutes: 600),
                policy: policy);

            Assert.True(expectedEffect.HasAnyEffect);
            Assert.True(result.HasAnyEffect);
            Assert.Equal(
                expected: before.Energy + expectedEffect.EnergyDelta,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: before.Stress + expectedEffect.StressDelta,
                actual: resident.Stress.Value);
            Assert.Equal(
                expected: before.SocialNeed + expectedEffect.SocialNeedDelta,
                actual: resident.SocialNeed.Value);
            Assert.Equal(before.Health, resident.Health.Value);
            Assert.Equal(expectedEffect.HealthDelta, result.ExternalHealthDelta);
            Assert.Equal(
                expected: before.Happiness + expectedEffect.HappinessDelta,
                actual: resident.Happiness.Value);
        }

        [Fact]
        public void Apply_WhenEnvironmentHasUtcOffset_UsesOffsetForProgressionCalculation()
        {
            Person resident = CreatePerson();
            var policy = new PersonNeedsProgressionPolicy();
            DateTimeOffset fromUtc = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 13,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            DateTimeOffset toUtc = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 15,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            PersonNeedsProgressionEffect expectedEffectWithOffset = policy.Calculate(
                person: resident,
                fromSimTimeUtc: fromUtc,
                toSimTimeUtc: toUtc,
                utcOffsetMinutes: 600);
            PersonNeedsProgressionEffect expectedEffectWithoutOffset = policy.Calculate(
                person: resident,
                fromSimTimeUtc: fromUtc,
                toSimTimeUtc: toUtc,
                utcOffsetMinutes: 0);
            var before = NeedsSnapshot.Capture(resident);

            ResidentProgressionStepResult result = Apply(
                resident: resident,
                fromUtc: fromUtc,
                toUtc: toUtc,
                environment: CreateEnvironment(utcOffsetMinutes: 600),
                policy: policy);

            Assert.NotEqual(
                expected: expectedEffectWithoutOffset,
                actual: expectedEffectWithOffset);
            Assert.True(result.HasAnyEffect);
            Assert.Equal(
                expected: before.Energy + expectedEffectWithOffset.EnergyDelta,
                actual: resident.Energy.Value);
            Assert.NotEqual(
                expected: before.Energy + expectedEffectWithoutOffset.EnergyDelta,
                actual: resident.Energy.Value);
        }

        [Fact]
        public void Apply_WhenResidentIsAlreadyDead_ReturnsFalse()
        {
            Person resident = CreatePerson();
            resident.Die(CurrentDate);
            var before = NeedsSnapshot.Capture(resident);

            ResidentProgressionStepResult result = Apply(resident: resident);

            Assert.False(result.HasAnyEffect);
            Assert.Equal(
                expected: before,
                actual: NeedsSnapshot.Capture(resident));
            Assert.False(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenNeedsPressureIsLethal_DelegatesHealthWithoutKillingResident()
        {
            var marriageDomainService = new MarriageDomainService();
            var householdId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            Person spouse = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: householdId,
                sex: Sex.Female,
                firstName: "Trinity",
                lastName: "Matrix",
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 6),
                currentDate: CurrentDate);
            Person resident = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 6),
                currentDate: CurrentDate,
                energy: 0,
                stress: 100,
                socialNeed: 100,
                health: 1);
            marriageDomainService.RegisterMarriage(
                person: resident,
                spouse: spouse,
                currentDate: CurrentDate);

            ResidentProgressionStepResult result = Apply(
                resident: resident,
                fromUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 20,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                policy: new PersonNeedsProgressionPolicy());

            Assert.True(result.HasAnyEffect);
            Assert.True(result.ExternalHealthDelta < 0);
            Assert.True(resident.IsAlive);
            Assert.Equal(MaritalStatus.Married, spouse.MaritalStatus);
            Assert.Equal(resident.Id, spouse.SpouseId);
        }

        private static ResidentProgressionStepResult Apply(
            Person resident,
            DateTimeOffset? fromUtc = null,
            DateTimeOffset? toUtc = null,
            CityPopulationEnvironment? environment = null,
            PersonNeedsProgressionPolicy? policy = null)
        {
            return ResidentNeedsProgressionStep.Apply(
                person: resident,
                fromSimTimeUtc: fromUtc ?? FromUtc,
                toSimTimeUtc: toUtc ?? ToUtc,
                environment: environment,
                personNeedsProgressionPolicy: policy ?? new PersonNeedsProgressionPolicy());
        }

        private static CityPopulationEnvironment CreateEnvironment(int utcOffsetMinutes)
        {
            return CityPopulationEnvironment.Create(
                cityId: CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                climateZone: PopulationClimateZone.Temperate,
                hemisphere: PopulationHemisphere.Northern,
                utcOffsetMinutes: utcOffsetMinutes,
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 1,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private sealed record NeedsSnapshot(
            int Energy,
            int Stress,
            int SocialNeed,
            int Health,
            int Happiness)
        {
            public static NeedsSnapshot Capture(Person person)
            {
                return new NeedsSnapshot(
                    Energy: person.Energy.Value,
                    Stress: person.Stress.Value,
                    SocialNeed: person.SocialNeed.Value,
                    Health: person.Health.Value,
                    Happiness: person.Happiness.Value);
            }
        }
    }
}
