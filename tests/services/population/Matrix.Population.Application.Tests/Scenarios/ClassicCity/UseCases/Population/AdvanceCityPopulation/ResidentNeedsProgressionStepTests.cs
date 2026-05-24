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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class ResidentNeedsProgressionStepTests
{
    private static readonly DateOnly CurrentDate = new(2048, 5, 6);
    private static readonly DateTimeOffset FromUtc = new(2048, 5, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ToUtc = FromUtc.AddHours(4);

    [Fact]
    public void Apply_WhenToTimeIsNotAfterFromTime_ReturnsFalseAndDoesNotChangeNeeds()
    {
        Person resident = CreatePerson();
        NeedsSnapshot before = NeedsSnapshot.Capture(resident);

        bool changed = Apply(
            resident: resident,
            fromUtc: FromUtc,
            toUtc: FromUtc);

        Assert.False(changed);
        Assert.Equal(before, NeedsSnapshot.Capture(resident));
        Assert.True(resident.IsAlive);
    }

    [Fact]
    public void Apply_WhenTimeAdvances_AppliesCalculatedNeedsProgressionEffect()
    {
        Person resident = CreatePerson();
        var policy = new PersonNeedsProgressionPolicy();
        NeedsSnapshot before = NeedsSnapshot.Capture(resident);
        PersonNeedsProgressionEffect expectedEffect = policy.Calculate(
            person: resident,
            fromSimTimeUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero),
            toSimTimeUtc: new DateTimeOffset(2048, 5, 6, 15, 0, 0, TimeSpan.Zero),
            utcOffsetMinutes: 600);

        bool changed = Apply(
            resident: resident,
            fromUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero),
            toUtc: new DateTimeOffset(2048, 5, 6, 15, 0, 0, TimeSpan.Zero),
            environment: CreateEnvironment(utcOffsetMinutes: 600),
            policy: policy);

        Assert.True(expectedEffect.HasAnyEffect);
        Assert.True(changed);
        Assert.Equal(before.Energy + expectedEffect.EnergyDelta, resident.Energy.Value);
        Assert.Equal(before.Stress + expectedEffect.StressDelta, resident.Stress.Value);
        Assert.Equal(before.SocialNeed + expectedEffect.SocialNeedDelta, resident.SocialNeed.Value);
        Assert.Equal(before.Health + expectedEffect.HealthDelta, resident.Health.Value);
        Assert.Equal(before.Happiness + expectedEffect.HappinessDelta, resident.Happiness.Value);
    }

    [Fact]
    public void Apply_WhenEnvironmentHasUtcOffset_UsesOffsetForProgressionCalculation()
    {
        Person resident = CreatePerson();
        var policy = new PersonNeedsProgressionPolicy();
        DateTimeOffset fromUtc = new(2048, 5, 6, 13, 0, 0, TimeSpan.Zero);
        DateTimeOffset toUtc = new(2048, 5, 6, 15, 0, 0, TimeSpan.Zero);
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
        NeedsSnapshot before = NeedsSnapshot.Capture(resident);

        bool changed = Apply(
            resident: resident,
            fromUtc: fromUtc,
            toUtc: toUtc,
            environment: CreateEnvironment(utcOffsetMinutes: 600),
            policy: policy);

        Assert.NotEqual(expectedEffectWithoutOffset, expectedEffectWithOffset);
        Assert.True(changed);
        Assert.Equal(before.Energy + expectedEffectWithOffset.EnergyDelta, resident.Energy.Value);
        Assert.NotEqual(before.Energy + expectedEffectWithoutOffset.EnergyDelta, resident.Energy.Value);
    }

    [Fact]
    public void Apply_WhenResidentIsAlreadyDead_ReturnsFalse()
    {
        Person resident = CreatePerson();
        resident.Die(CurrentDate);
        NeedsSnapshot before = NeedsSnapshot.Capture(resident);

        bool changed = Apply(resident: resident);

        Assert.False(changed);
        Assert.Equal(before, NeedsSnapshot.Capture(resident));
        Assert.False(resident.IsAlive);
    }

    [Fact]
    public void Apply_WhenNeedsProgressionKillsMarriedResident_RegistersSpouseWidowhood()
    {
        var marriageDomainService = new MarriageDomainService();
        Guid householdId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        Person spouse = CreatePerson(
            personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            householdId: householdId,
            sex: Sex.Female,
            firstName: "Trinity",
            lastName: "Matrix",
            birthDate: new DateOnly(2020, 5, 6),
            currentDate: CurrentDate);
        Person resident = CreatePerson(
            personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            householdId: householdId,
            sex: Sex.Male,
            birthDate: new DateOnly(2020, 5, 6),
            currentDate: CurrentDate,
            energy: 0,
            stress: 100,
            socialNeed: 100,
            health: 1);
        marriageDomainService.RegisterMarriage(
            person: resident,
            spouse: spouse,
            currentDate: CurrentDate);

        bool changed = Apply(
            resident: resident,
            residentsById: new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident,
                [spouse.Id] = spouse
            },
            fromUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            toUtc: new DateTimeOffset(2048, 5, 6, 20, 0, 0, TimeSpan.Zero),
            marriageDomainService: marriageDomainService);

        Assert.True(changed);
        Assert.False(resident.IsAlive);
        Assert.Equal(MaritalStatus.Widowed, spouse.MaritalStatus);
        Assert.Null(spouse.SpouseId);
    }

    private static bool Apply(
        Person resident,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CityPopulationEnvironment? environment = null,
        IReadOnlyDictionary<PersonId, Person>? residentsById = null,
        MarriageDomainService? marriageDomainService = null,
        PersonNeedsProgressionPolicy? policy = null)
    {
        return ResidentNeedsProgressionStep.Apply(
            person: resident,
            residentsById: residentsById ?? new Dictionary<PersonId, Person>
            {
                [resident.Id] = resident
            },
            fromSimTimeUtc: fromUtc ?? FromUtc,
            toSimTimeUtc: toUtc ?? ToUtc,
            currentDate: CurrentDate,
            environment: environment,
            marriageDomainService: marriageDomainService ?? new MarriageDomainService(),
            personNeedsProgressionPolicy: policy ?? new PersonNeedsProgressionPolicy());
    }

    private static CityPopulationEnvironment CreateEnvironment(int utcOffsetMinutes)
    {
        return CityPopulationEnvironment.Create(
            cityId: CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            climateZone: PopulationClimateZone.Temperate,
            hemisphere: PopulationHemisphere.Northern,
            utcOffsetMinutes: utcOffsetMinutes,
            createdAtUtc: new DateTimeOffset(2048, 1, 1, 0, 0, 0, TimeSpan.Zero));
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
