using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationHealthcarePressurePolicy
    {
        public CityPopulationHealthcarePressureProfile Evaluate(
            IReadOnlyCollection<Person> residents,
            CityPopulationServiceQualityState? serviceQualityState = null,
            CityPopulationLivingConditionsState? livingConditionsState = null,
            CityPopulationEssentialsState? essentialsState = null)
        {
            ArgumentNullException.ThrowIfNull(residents);

            Person[] aliveResidents = residents
               .Where(x => x.IsAlive)
               .ToArray();

            if (aliveResidents.Length == 0)
                return new CityPopulationHealthcarePressureProfile(
                    ActiveIllnessCount: 0,
                    SevereIllnessCount: 0,
                    MedicalLoadIndex: 0.20m,
                    TriagePressureIndex: 0m,
                    RecoverySupportIndex: 1m);

            int activeIllnessCount = 0;
            int mildIllnessCount = 0;
            int moderateIllnessCount = 0;
            int severeIllnessCount = 0;

            foreach (Person resident in aliveResidents)
            {
                if (!resident.HasActiveIllness)
                    continue;

                activeIllnessCount++;

                switch (resident.CurrentIllnessSeverity ?? IllnessSeverity.Mild)
                {
                    case IllnessSeverity.Mild:
                        mildIllnessCount++;
                        break;
                    case IllnessSeverity.Moderate:
                        moderateIllnessCount++;
                        break;
                    case IllnessSeverity.Severe:
                        severeIllnessCount++;
                        break;
                }
            }

            decimal residentCount = aliveResidents.Length;
            decimal healthcareQualityIndex = serviceQualityState?.HealthcareQualityIndex ?? 1m;
            decimal medicineStockSupport = ResolvePositiveSupport(
                essentialsState?.MedicineStockLevelIndex ?? 1m);
            decimal medicineShortagePressure = ResolvePressureExcess(
                essentialsState?.MedicineShortageRiskIndex ?? 1m);
            decimal emergencyWaterShortagePressure = ResolvePressureExcess(
                essentialsState?.EmergencyWaterShortageRiskIndex ?? 1m);
            decimal roadAccessDeficit = ResolveCoverageDeficit(
                livingConditionsState?.RoadAccessibilityIndex ?? 1m);
            decimal powerCoverageDeficit = ResolveCoverageDeficit(
                livingConditionsState?.PowerCoverageIndex ?? 1m);
            decimal utilityContinuityDeficit = ResolveCoverageDeficit(
                livingConditionsState?.UtilityContinuityIndex ?? 1m);
            decimal sanitationCoverageDeficit = ResolveCoverageDeficit(
                livingConditionsState?.SanitationCoverageIndex ?? 1m);

            decimal weightedIllnessLoad = ((mildIllnessCount * 0.85m) +
                                           (moderateIllnessCount * 1.55m) +
                                           (severeIllnessCount * 2.75m)) /
                                          residentCount;

            decimal effectiveCapacity = Clamp(
                value: 0.40m +
                       (healthcareQualityIndex * 0.72m) +
                       (medicineStockSupport * 0.22m) -
                       (medicineShortagePressure * 0.38m) -
                       (roadAccessDeficit * 0.24m) -
                       (powerCoverageDeficit * 0.12m) -
                       (utilityContinuityDeficit * 0.10m) -
                       (sanitationCoverageDeficit * 0.10m) -
                       (emergencyWaterShortagePressure * 0.08m),
                min: 0.25m,
                max: 2.40m);

            decimal overloadPressure = Math.Max(
                val1: 0m,
                val2: (weightedIllnessLoad * 4.20m) - effectiveCapacity);
            decimal severeCaseShare = severeIllnessCount / residentCount;

            decimal medicalLoadIndex = RoundIndex(
                Clamp(
                    value: 0.20m +
                           (weightedIllnessLoad * 3.60m) +
                           (overloadPressure * 0.65m) +
                           (medicineShortagePressure * 0.24m),
                    min: 0.20m,
                    max: 3m));
            decimal triagePressureIndex = RoundIndex(
                Clamp(
                    value: (severeCaseShare * 4.40m) +
                           (overloadPressure * 0.90m) +
                           (medicineShortagePressure * 0.35m) +
                           (roadAccessDeficit * 0.18m),
                    min: 0m,
                    max: 3m));
            decimal recoverySupportIndex = RoundIndex(
                Clamp(
                    value: effectiveCapacity -
                           (overloadPressure * 0.28m) -
                           (medicineShortagePressure * 0.20m) +
                           (medicineStockSupport * 0.08m),
                    min: 0.25m,
                    max: 1.75m));

            return new CityPopulationHealthcarePressureProfile(
                ActiveIllnessCount: activeIllnessCount,
                SevereIllnessCount: severeIllnessCount,
                MedicalLoadIndex: medicalLoadIndex,
                TriagePressureIndex: triagePressureIndex,
                RecoverySupportIndex: recoverySupportIndex);
        }

        private static decimal ResolveCoverageDeficit(decimal index)
        {
            return Clamp(
                value: 1m - index,
                min: 0m,
                max: 1m);
        }

        private static decimal ResolvePositiveSupport(decimal index)
        {
            return Clamp(
                value: index - 1m,
                min: 0m,
                max: 1m);
        }

        private static decimal ResolvePressureExcess(decimal index)
        {
            return Clamp(
                value: index - 1m,
                min: 0m,
                max: 1m);
        }

        private static decimal Clamp(
            decimal value,
            decimal min,
            decimal max)
        {
            return value < min
                ? min
                : value > max
                    ? max
                    : value;
        }

        private static decimal RoundIndex(decimal value)
        {
            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
