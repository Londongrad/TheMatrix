using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationLivingConditionsPressurePolicy
    {
        public CityPopulationLivingConditionsPressureEffect Calculate(
            Person person,
            DateOnly previousDate,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (!person.IsAlive || currentDate <= previousDate)
                return CityPopulationLivingConditionsPressureEffect.None;

            int daysElapsed = Math.Max(
                val1: 0,
                val2: currentDate.DayNumber - previousDate.DayNumber);
            if (daysElapsed <= 0)
                return CityPopulationLivingConditionsPressureEffect.None;

            double heatingDeficit = ResolveCoverageDeficit(livingConditions.HeatingCoverageIndex);
            double waterDeficit = ResolveCoverageDeficit(livingConditions.WaterCoverageIndex);
            double sanitationDeficit = ResolveCoverageDeficit(livingConditions.SanitationCoverageIndex);
            double powerDeficit = ResolveCoverageDeficit(livingConditions.PowerCoverageIndex);
            double continuityDeficit = ResolveCoverageDeficit(livingConditions.UtilityContinuityIndex);
            double roadDeficit = ResolveCoverageDeficit(livingConditions.RoadAccessibilityIndex);
            double floodingPressure = ResolvePressure(livingConditions.FloodingIndex);

            double foodShortage = ResolvePressure(essentials.FoodShortageRiskIndex);
            double medicineShortage = ResolvePressure(essentials.MedicineShortageRiskIndex);
            double emergencyWaterShortage = ResolvePressure(essentials.EmergencyWaterShortageRiskIndex);
            double rationingPressure = essentials.EmergencyRationingEnabled
                ? 0.25d
                : 0d;

            double ageVulnerability = person.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior
                ? 1.20d
                : 1.00d;
            double housingVulnerability = housingStatus == HousingStatus.Homeless
                ? 1.25d
                : 1.00d;
            double activityVulnerability =
                person.Employment.Status is EmploymentStatus.Employed or EmploymentStatus.Student
                    ? 1.15d
                    : 0.75d;
            double functionalVulnerability = 1d +
                                             (((100d - person.FunctionalCapacity.Value) / 100d) * 0.20d);

            double healthLoss = daysElapsed *
                                (
                                    (heatingDeficit * 1.20d * ageVulnerability * housingVulnerability) +
                                    (waterDeficit * 1.00d * housingVulnerability) +
                                    (sanitationDeficit * 0.85d) +
                                    (floodingPressure * 0.70d) +
                                    (foodShortage * 1.40d * ageVulnerability) +
                                    (emergencyWaterShortage * 1.00d) +
                                    (medicineShortage * 0.45d * functionalVulnerability));

            double energyLoss = daysElapsed *
                                (
                                    (heatingDeficit * 4.00d * housingVulnerability) +
                                    (powerDeficit * 2.50d) +
                                    (foodShortage * 5.00d) +
                                    (emergencyWaterShortage * 2.00d) +
                                    (roadDeficit * 1.50d * activityVulnerability));

            double stressGain = daysElapsed *
                                (
                                    (powerDeficit * 3.00d) +
                                    (waterDeficit * 2.00d) +
                                    (sanitationDeficit * 1.80d) +
                                    (roadDeficit * 2.40d * activityVulnerability) +
                                    (floodingPressure * 3.50d) +
                                    (continuityDeficit * 1.80d) +
                                    (rationingPressure * 1.60d) +
                                    (foodShortage * 2.50d));

            double happinessLoss = daysElapsed *
                                   (
                                       (powerDeficit * 2.00d) +
                                       (waterDeficit * 1.50d) +
                                       (floodingPressure * 2.40d) +
                                       (foodShortage * 2.80d) +
                                       (rationingPressure * 1.30d) +
                                       (heatingDeficit * 1.20d));

            return new CityPopulationLivingConditionsPressureEffect(
                HealthDelta: -ClampLoss(
                    value: healthLoss,
                    maxMagnitude: 10),
                EnergyDelta: -ClampLoss(
                    value: energyLoss,
                    maxMagnitude: 18),
                StressDelta: ClampLoss(
                    value: stressGain,
                    maxMagnitude: 18),
                HappinessDelta: -ClampLoss(
                    value: happinessLoss,
                    maxMagnitude: 14));
        }

        public double ResolvePublicHealthRiskStrength(
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials)
        {
            double waterDeficit = ResolveCoverageDeficit(livingConditions.WaterCoverageIndex);
            double sanitationDeficit = ResolveCoverageDeficit(livingConditions.SanitationCoverageIndex);
            double floodingPressure = ResolvePressure(livingConditions.FloodingIndex);
            double emergencyWaterShortage = ResolvePressure(essentials.EmergencyWaterShortageRiskIndex);
            double foodShortage = ResolvePressure(essentials.FoodShortageRiskIndex) * 0.35d;

            double blended = (waterDeficit * 0.28d) +
                             (sanitationDeficit * 0.28d) +
                             (floodingPressure * 0.22d) +
                             (emergencyWaterShortage * 0.17d) +
                             foodShortage;

            return Math.Clamp(
                value: blended,
                min: 0d,
                max: 1d);
        }

        public double ResolveMedicineAccessStrength(
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials)
        {
            double medicineShortage = ResolvePressure(essentials.MedicineShortageRiskIndex);
            double continuityDeficit = ResolveCoverageDeficit(livingConditions.UtilityContinuityIndex);

            double access = 1d - (medicineShortage * 0.75d) - (continuityDeficit * 0.15d);

            if (essentials.EmergencyRationingEnabled)
                access -= 0.05d;

            return Math.Clamp(
                value: access,
                min: 0.25d,
                max: 1d);
        }

        private static double ResolveCoverageDeficit(decimal value)
        {
            return Math.Clamp(
                value: (double)(1m - value),
                min: 0d,
                max: 1.50d);
        }

        private static double ResolvePressure(decimal value)
        {
            return Math.Clamp(
                value: (double)value,
                min: 0d,
                max: 1.50d);
        }

        private static int ClampLoss(
            double value,
            int maxMagnitude)
        {
            int rounded = (int)Math.Round(
                value: value,
                mode: MidpointRounding.AwayFromZero);
            return Math.Clamp(
                value: rounded,
                min: 0,
                max: maxMagnitude);
        }
    }
}
