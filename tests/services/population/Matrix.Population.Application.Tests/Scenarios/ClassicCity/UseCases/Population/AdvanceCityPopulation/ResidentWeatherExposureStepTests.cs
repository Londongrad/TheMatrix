using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class ResidentWeatherExposureStepTests
    {
        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 7,
            day: 10);

        private static readonly DateTimeOffset DayStartUtc = new(
            year: 2048,
            month: 7,
            day: 10,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void Apply_WhenExposureSegmentsAreEmpty_ReturnsFalseAndDoesNotChangeResident()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments: []);

            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: WeatherSnapshot.Capture(resident));
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenPolicyReturnsNoEffect_ReturnsFalseAndDoesNotChangeResident()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            CityWeatherExposureSegment segment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Clear,
                    severity: PopulationWeatherSeverity.Mild,
                    temperatureC: 22m));
            CityPopulationWeatherExposurePolicy policy = CreatePolicy();
            PersonWeatherImpact expectedImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: segment,
                environment: null);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments: [segment],
                policy: policy);

            Assert.False(expectedImpact.HasEffect);
            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: WeatherSnapshot.Capture(resident));
        }

        [Fact]
        public void Apply_WhenAdverseHeatwaveSegmentHasImpact_AppliesHealthAndHappinessDeltas()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: CurrentDate);
            CityWeatherExposureSegment segment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m));
            CityPopulationWeatherExposurePolicy policy = CreatePolicy();
            PersonWeatherImpact expectedImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: segment,
                environment: null);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments: [segment],
                policy: policy);

            Assert.True(expectedImpact.HasEffect);
            Assert.True(changed);
            Assert.Equal(
                expected: before.Health + expectedImpact.HealthDelta,
                actual: resident.Health.Value);
            Assert.Equal(
                expected: before.Happiness + expectedImpact.HappinessDelta,
                actual: resident.Happiness.Value);
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenMultipleSegmentsHaveImpacts_AggregatesDeltasBeforeApplying()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: CurrentDate);
            CityWeatherExposureSegment adverseSegment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m));
            CityWeatherExposureSegment recoverySegment = CreateRecoverySegment(
                recoveryWeather: CreateWeather(
                    type: PopulationWeatherType.Clear,
                    severity: PopulationWeatherSeverity.Mild,
                    temperatureC: 22m,
                    humidityPercent: 50m,
                    windSpeedKph: 10m),
                sourceWeather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m));
            CityPopulationWeatherExposurePolicy policy = CreatePolicy();
            PersonWeatherImpact adverseImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: adverseSegment,
                environment: null);
            PersonWeatherImpact recoveryImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: recoverySegment,
                environment: null);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments:
                [
                    adverseSegment,
                    recoverySegment
                ],
                policy: policy);

            Assert.True(adverseImpact.HasEffect);
            Assert.True(recoveryImpact.HasEffect);
            Assert.True(changed);
            Assert.Equal(
                expected: before.Health + adverseImpact.HealthDelta + recoveryImpact.HealthDelta,
                actual: resident.Health.Value);
            Assert.Equal(
                expected: before.Happiness + adverseImpact.HappinessDelta + recoveryImpact.HappinessDelta,
                actual: resident.Happiness.Value);
        }

        [Fact]
        public void Apply_WhenEnvironmentReducesWeatherImpact_UsesEnvironmentInCalculation()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            CityWeatherExposureSegment segment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Severe,
                    temperatureC: 40m),
                intervalEndUtc: DayStartUtc.AddHours(18));
            CityPopulationEnvironment environment = CreateEnvironment(
                climateZone: PopulationClimateZone.Tropical,
                hemisphere: PopulationHemisphere.Northern);
            CityPopulationWeatherExposurePolicy policy = CreatePolicy();
            PersonWeatherImpact baselineImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: segment,
                environment: null);
            PersonWeatherImpact adaptedImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: segment,
                environment: environment);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments: [segment],
                environment: environment,
                policy: policy);

            Assert.NotEqual(
                expected: baselineImpact,
                actual: adaptedImpact);
            Assert.False(adaptedImpact.HasEffect);
            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: WeatherSnapshot.Capture(resident));
        }

        [Fact]
        public void Apply_WhenResidentIsAlreadyDead_ReturnsFalse()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            resident.Die(CurrentDate);
            var before = WeatherSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                exposureSegments:
                [
                    CreateAdverseSegment(
                        weather: CreateWeather(
                            type: PopulationWeatherType.Heatwave,
                            severity: PopulationWeatherSeverity.Extreme,
                            temperatureC: 39m))
                ]);

            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: WeatherSnapshot.Capture(resident));
            Assert.False(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenHealthDeltaKillsResident_DoesNotApplyHappinessDelta()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: CurrentDate,
                health: 1,
                happiness: 50);
            CityWeatherExposureSegment segment = CreateAdverseSegment(
                weather: CreateWeather(
                    type: PopulationWeatherType.Heatwave,
                    severity: PopulationWeatherSeverity.Extreme,
                    temperatureC: 39m));
            CityPopulationWeatherExposurePolicy policy = CreatePolicy();
            PersonWeatherImpact expectedImpact = policy.Calculate(
                person: resident,
                currentDate: CurrentDate,
                segment: segment,
                environment: null);
            int previousHappiness = resident.Happiness.Value;

            bool changed = Apply(
                resident: resident,
                exposureSegments: [segment],
                policy: policy);

            Assert.True(expectedImpact.HealthDelta < 0);
            Assert.True(expectedImpact.HappinessDelta < 0);
            Assert.True(changed);
            Assert.False(resident.IsAlive);
            Assert.Equal(
                expected: previousHappiness,
                actual: resident.Happiness.Value);
        }

        [Fact]
        public void Apply_WhenWeatherExposureKillsMarriedResident_RegistersSpouseWidowhood()
        {
            var marriageDomainService = new MarriageDomainService();
            var householdId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            Person spouse = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: householdId,
                sex: Sex.Female,
                firstName: "Trinity",
                lastName: "Matrix",
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: CurrentDate);
            Person resident = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(
                    year: 1960,
                    month: 7,
                    day: 10),
                currentDate: CurrentDate,
                health: 1);
            marriageDomainService.RegisterMarriage(
                person: resident,
                spouse: spouse,
                currentDate: CurrentDate);

            bool changed = Apply(
                resident: resident,
                exposureSegments:
                [
                    CreateAdverseSegment(
                        weather: CreateWeather(
                            type: PopulationWeatherType.Heatwave,
                            severity: PopulationWeatherSeverity.Extreme,
                            temperatureC: 39m))
                ],
                residentsById: new Dictionary<PersonId, Person>
                {
                    [resident.Id] = resident,
                    [spouse.Id] = spouse
                },
                marriageDomainService: marriageDomainService);

            Assert.True(changed);
            Assert.False(resident.IsAlive);
            Assert.Equal(
                expected: MaritalStatus.Widowed,
                actual: spouse.MaritalStatus);
            Assert.Null(spouse.SpouseId);
        }

        private static bool Apply(
            Person resident,
            IReadOnlyCollection<CityWeatherExposureSegment> exposureSegments,
            CityPopulationEnvironment? environment = null,
            IReadOnlyDictionary<PersonId, Person>? residentsById = null,
            MarriageDomainService? marriageDomainService = null,
            CityPopulationWeatherExposurePolicy? policy = null)
        {
            return ResidentWeatherExposureStep.Apply(
                person: resident,
                residentsById: residentsById ??
                               new Dictionary<PersonId, Person>
                               {
                                   [resident.Id] = resident
                               },
                currentDate: CurrentDate,
                environment: environment,
                exposureSegments: exposureSegments,
                marriageDomainService: marriageDomainService ?? new MarriageDomainService(),
                weatherExposurePolicy: policy ?? CreatePolicy());
        }

        private static CityPopulationWeatherExposurePolicy CreatePolicy()
        {
            return new CityPopulationWeatherExposurePolicy(
                climateAdaptationPolicy: new CityPopulationClimateAdaptationPolicy());
        }

        private static CityWeatherExposureSegment CreateAdverseSegment(
            WeatherImpactProfile weather,
            DateTimeOffset? effectStartedAtUtc = null,
            DateTimeOffset? intervalStartUtc = null,
            DateTimeOffset? intervalEndUtc = null)
        {
            return new CityWeatherExposureSegment(
                Kind: CityWeatherExposureKind.Adverse,
                Weather: weather,
                EffectStartedAtSimTimeUtc: effectStartedAtUtc ?? DayStartUtc,
                IntervalStartSimTimeUtc: intervalStartUtc ?? DayStartUtc,
                IntervalEndSimTimeUtc: intervalEndUtc ?? DayStartUtc.AddHours(6));
        }

        private static CityWeatherExposureSegment CreateRecoverySegment(
            WeatherImpactProfile recoveryWeather,
            WeatherImpactProfile sourceWeather,
            DateTimeOffset? effectStartedAtUtc = null,
            DateTimeOffset? intervalStartUtc = null,
            DateTimeOffset? intervalEndUtc = null)
        {
            return new CityWeatherExposureSegment(
                Kind: CityWeatherExposureKind.Recovery,
                Weather: recoveryWeather,
                EffectStartedAtSimTimeUtc: effectStartedAtUtc ?? DayStartUtc.AddHours(18),
                IntervalStartSimTimeUtc: intervalStartUtc ?? DayStartUtc.AddHours(18),
                IntervalEndSimTimeUtc: intervalEndUtc ??
                                       DayStartUtc.AddDays(1)
                                          .AddHours(6),
                SourceWeather: sourceWeather);
        }

        private static WeatherImpactProfile CreateWeather(
            PopulationWeatherType type,
            PopulationWeatherSeverity severity,
            decimal temperatureC,
            decimal humidityPercent = 45m,
            decimal windSpeedKph = 12m)
        {
            return new WeatherImpactProfile(
                Type: type,
                Severity: severity,
                PrecipitationKind: PopulationPrecipitationKind.None,
                TemperatureC: temperatureC,
                HumidityPercent: humidityPercent,
                WindSpeedKph: windSpeedKph,
                CloudCoveragePercent: 35m,
                PressureHpa: 1012m);
        }

        private static CityPopulationEnvironment CreateEnvironment(
            PopulationClimateZone climateZone,
            PopulationHemisphere hemisphere)
        {
            return CityPopulationEnvironment.Create(
                cityId: CityId.From(Guid.Parse("34343434-3434-3434-3434-343434343434")),
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffsetMinutes: 0,
                createdAtUtc: DayStartUtc);
        }

        private sealed record WeatherSnapshot(
            int Health,
            int Happiness)
        {
            public static WeatherSnapshot Capture(Person person)
            {
                return new WeatherSnapshot(
                    Health: person.Health.Value,
                    Happiness: person.Happiness.Value);
            }
        }
    }
}
