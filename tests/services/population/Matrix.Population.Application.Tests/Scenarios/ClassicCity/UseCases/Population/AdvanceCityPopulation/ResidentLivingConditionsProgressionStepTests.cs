using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
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
    public sealed class ResidentLivingConditionsProgressionStepTests
    {
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        private static readonly DistrictId TestDistrictId =
            DistrictId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        private static readonly DateOnly PreviousDate = new(
            year: 2048,
            month: 5,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 3);

        private static readonly DateTimeOffset EffectiveAtUtc = new(
            year: 2048,
            month: 5,
            day: 3,
            hour: 12,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void Apply_WhenCurrentDateDoesNotAdvance_ReturnsFalseAndDoesNotChangeResident()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            var before = NeedsSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                previousDate: CurrentDate,
                currentDate: CurrentDate);

            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: NeedsSnapshot.Capture(resident));
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenNoLivingConditionStateExists_ReturnsFalse()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            var before = NeedsSnapshot.Capture(resident);

            bool changed = Apply(resident: resident);

            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: NeedsSnapshot.Capture(resident));
            Assert.True(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenLivingConditionsAreDegraded_AppliesPressureEffect()
        {
            Person resident = CreatePerson(
                birthDate: new DateOnly(
                    year: 2040,
                    month: 5,
                    day: 1),
                currentDate: CurrentDate);
            resident.DiagnoseIllness(
                kind: IllnessKind.Infection,
                severity: IllnessSeverity.Mild,
                currentDate: CurrentDate);
            CityPopulationLivingConditionsState livingConditionsState = CreateDegradedLivingConditionsState();
            CityPopulationEssentialsState essentialsState = CreateDegradedEssentialsState();
            var districtImpactPolicy = new CityPopulationDistrictImpactPolicy();
            var livingConditionsPressurePolicy = new CityPopulationLivingConditionsPressurePolicy();
            CityPopulationLivingConditionsContext expectedLivingConditions =
                districtImpactPolicy.ResolveLivingConditions(
                    districtId: null,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditions: null);
            CityPopulationEssentialsContext expectedEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: null,
                essentialsState: essentialsState);
            CityPopulationLivingConditionsPressureEffect expectedEffect = livingConditionsPressurePolicy.Calculate(
                person: resident,
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                housingStatus: HousingStatus.Homeless,
                livingConditions: expectedLivingConditions,
                essentials: expectedEssentials);
            var before = NeedsSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [resident.HouseholdId] = HousingStatus.Homeless
                },
                livingConditionsState: livingConditionsState,
                essentialsState: essentialsState,
                districtImpactPolicy: districtImpactPolicy,
                livingConditionsPressurePolicy: livingConditionsPressurePolicy);

            Assert.True(expectedEffect.HasAnyEffect);
            Assert.True(changed);
            Assert.Equal(
                expected: before.Health + expectedEffect.HealthDelta,
                actual: resident.Health.Value);
            Assert.Equal(
                expected: before.Energy + expectedEffect.EnergyDelta,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: before.Stress + expectedEffect.StressDelta,
                actual: resident.Stress.Value);
            Assert.Equal(
                expected: before.Happiness + expectedEffect.HappinessDelta,
                actual: resident.Happiness.Value);
        }

        [Fact]
        public void Apply_WhenDistrictUtilitySnapshotExists_UsesDistrictUtilityConditions()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            CityPopulationLivingConditionsState livingConditionsState = CreateHealthyLivingConditionsState();
            CityDistrictUtilityConditionsSnapshot utilitySnapshot =
                CreateSevereUtilityConditionsSnapshot(TestDistrictId);
            var districtImpactPolicy = new CityPopulationDistrictImpactPolicy();
            var livingConditionsPressurePolicy = new CityPopulationLivingConditionsPressurePolicy();
            CityPopulationLivingConditionsContext expectedLivingConditions =
                districtImpactPolicy.ResolveLivingConditions(
                    districtId: TestDistrictId,
                    livingConditionsState: livingConditionsState,
                    districtUtilityConditions: utilitySnapshot);
            CityPopulationEssentialsContext expectedEssentials = districtImpactPolicy.ResolveEssentials(
                districtId: TestDistrictId,
                essentialsState: null);
            CityPopulationLivingConditionsPressureEffect expectedEffect = livingConditionsPressurePolicy.Calculate(
                person: resident,
                previousDate: PreviousDate,
                currentDate: CurrentDate,
                housingStatus: HousingStatus.Housed,
                livingConditions: expectedLivingConditions,
                essentials: expectedEssentials);
            var before = NeedsSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [resident.HouseholdId] = HousingStatus.Housed
                },
                districtByHouseholdId: new Dictionary<HouseholdId, DistrictId?>
                {
                    [resident.HouseholdId] = TestDistrictId
                },
                livingConditionsState: livingConditionsState,
                districtUtilityConditionsByDistrictId: new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>
                {
                    [TestDistrictId] = utilitySnapshot
                },
                districtImpactPolicy: districtImpactPolicy,
                livingConditionsPressurePolicy: livingConditionsPressurePolicy);

            Assert.True(expectedEffect.HasAnyEffect);
            Assert.True(changed);
            Assert.Equal(
                expected: before.Health + expectedEffect.HealthDelta,
                actual: resident.Health.Value);
            Assert.Equal(
                expected: before.Energy + expectedEffect.EnergyDelta,
                actual: resident.Energy.Value);
            Assert.Equal(
                expected: before.Stress + expectedEffect.StressDelta,
                actual: resident.Stress.Value);
            Assert.Equal(
                expected: before.Happiness + expectedEffect.HappinessDelta,
                actual: resident.Happiness.Value);
        }

        [Fact]
        public void Apply_WhenResidentIsAlreadyDead_ReturnsFalse()
        {
            Person resident = CreatePerson(currentDate: CurrentDate);
            resident.Die(CurrentDate);
            var before = NeedsSnapshot.Capture(resident);

            bool changed = Apply(
                resident: resident,
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [resident.HouseholdId] = HousingStatus.Homeless
                },
                livingConditionsState: CreateDegradedLivingConditionsState(),
                essentialsState: CreateDegradedEssentialsState());

            Assert.False(changed);
            Assert.Equal(
                expected: before,
                actual: NeedsSnapshot.Capture(resident));
            Assert.False(resident.IsAlive);
        }

        [Fact]
        public void Apply_WhenLivingConditionsKillMarriedResident_RegistersSpouseWidowhood()
        {
            var marriageDomainService = new MarriageDomainService();
            var householdId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            Person spouse = CreatePerson(
                personId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                householdId: householdId,
                sex: Sex.Female,
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 1),
                currentDate: CurrentDate);
            Person resident = CreatePerson(
                personId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                householdId: householdId,
                sex: Sex.Male,
                birthDate: new DateOnly(
                    year: 2020,
                    month: 5,
                    day: 1),
                currentDate: CurrentDate,
                energy: 0,
                stress: 100,
                health: 1);
            marriageDomainService.RegisterMarriage(
                person: resident,
                spouse: spouse,
                currentDate: CurrentDate);

            bool changed = Apply(
                resident: resident,
                housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
                {
                    [resident.HouseholdId] = HousingStatus.Homeless
                },
                livingConditionsState: CreateDegradedLivingConditionsState(),
                essentialsState: CreateDegradedEssentialsState(),
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
            DateOnly? previousDate = null,
            DateOnly? currentDate = null,
            IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
            IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
            CityPopulationLivingConditionsState? livingConditionsState = null,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>?
                districtUtilityConditionsByDistrictId = null,
            CityPopulationEssentialsState? essentialsState = null,
            IReadOnlyDictionary<PersonId, Person>? residentsById = null,
            CityPopulationDistrictImpactPolicy? districtImpactPolicy = null,
            CityPopulationLivingConditionsPressurePolicy? livingConditionsPressurePolicy = null,
            MarriageDomainService? marriageDomainService = null)
        {
            return ResidentLivingConditionsProgressionStep.Apply(
                person: resident,
                residentsById: residentsById ??
                               new Dictionary<PersonId, Person>
                               {
                                   [resident.Id] = resident
                               },
                previousDate: previousDate ?? PreviousDate,
                currentDate: currentDate ?? CurrentDate,
                housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
                districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
                livingConditionsState: livingConditionsState,
                districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId ??
                                                       new Dictionary<DistrictId,
                                                           CityDistrictUtilityConditionsSnapshot>(),
                essentialsState: essentialsState,
                districtImpactPolicy: districtImpactPolicy ?? new CityPopulationDistrictImpactPolicy(),
                livingConditionsPressurePolicy: livingConditionsPressurePolicy ??
                                                new CityPopulationLivingConditionsPressurePolicy(),
                marriageDomainService: marriageDomainService ?? new MarriageDomainService());
        }

        private static CityPopulationLivingConditionsState CreateHealthyLivingConditionsState()
        {
            return CityPopulationLivingConditionsState.Create(
                cityId: TestCityId,
                floodingIndex: 0m,
                roadAccessibilityIndex: 1m,
                powerCoverageIndex: 1m,
                utilityContinuityIndex: 1m,
                heatingCoverageIndex: 1m,
                waterCoverageIndex: 1m,
                sanitationCoverageIndex: 1m,
                effectiveTickId: 10,
                effectiveAtUtc: EffectiveAtUtc,
                updatedAtUtc: EffectiveAtUtc);
        }

        private static CityPopulationLivingConditionsState CreateDegradedLivingConditionsState()
        {
            return CityPopulationLivingConditionsState.Create(
                cityId: TestCityId,
                floodingIndex: 0.8m,
                roadAccessibilityIndex: 0.5m,
                powerCoverageIndex: 0.7m,
                utilityContinuityIndex: 0.65m,
                heatingCoverageIndex: 0.4m,
                waterCoverageIndex: 0.5m,
                sanitationCoverageIndex: 0.6m,
                effectiveTickId: 10,
                effectiveAtUtc: EffectiveAtUtc,
                updatedAtUtc: EffectiveAtUtc);
        }

        private static CityPopulationEssentialsState CreateDegradedEssentialsState()
        {
            return CityPopulationEssentialsState.Create(
                cityId: TestCityId,
                supplyStressIndex: 1.4m,
                emergencyRationingEnabled: true,
                foodStockLevelIndex: 0.8m,
                foodShortageRiskIndex: 0.7m,
                medicineStockLevelIndex: 0.75m,
                medicineShortageRiskIndex: 0.6m,
                emergencyWaterStockLevelIndex: 0.7m,
                emergencyWaterShortageRiskIndex: 0.5m,
                effectiveTickId: 10,
                effectiveAtUtc: EffectiveAtUtc,
                updatedAtUtc: EffectiveAtUtc);
        }

        private static CityDistrictUtilityConditionsSnapshot CreateSevereUtilityConditionsSnapshot(
            DistrictId districtId)
        {
            return new CityDistrictUtilityConditionsSnapshot(
                DistrictId: districtId,
                HeatingCoverageIndex: 0.2m,
                HeatingComfortStressIndex: 0.9m,
                WaterCoverageIndex: 0.2m,
                WaterDisruptionRiskIndex: 0.9m,
                PowerCoverageIndex: 0.2m,
                PowerOutageRiskIndex: 0.9m,
                SanitationCoverageIndex: 0.2m,
                SanitationContaminationRiskIndex: 0.9m,
                UtilityIncidentDispatchReadinessIndex: 0.1m,
                UtilityIncidentPressureIndex: 0.9m,
                UtilityIncidentCoordinationDifficultyIndex: 0.8m,
                UtilityIncidentRestorationPriorityIndex: 0.8m);
        }

        private sealed record NeedsSnapshot(
            int Health,
            int Energy,
            int Stress,
            int Happiness)
        {
            public static NeedsSnapshot Capture(Person person)
            {
                return new NeedsSnapshot(
                    Health: person.Health.Value,
                    Energy: person.Energy.Value,
                    Stress: person.Stress.Value,
                    Happiness: person.Happiness.Value);
            }
        }
    }
}
