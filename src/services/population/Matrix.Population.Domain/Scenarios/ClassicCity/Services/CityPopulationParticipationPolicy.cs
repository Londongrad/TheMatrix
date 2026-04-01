using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationParticipationPolicy
    {
        public CityPopulationParticipationProfile ResolveEmploymentProfile(
            Person person,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            CityPopulationLivingConditionsState? livingConditionsState,
            CityPopulationEssentialsState? essentialsState)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (!person.IsAlive || person.Employment.Status != EmploymentStatus.Employed)
                return CityPopulationParticipationProfile.Full;

            double roadDeficit = ResolveCoverageDeficit(livingConditionsState?.RoadAccessibilityIndex ?? 1m);
            double powerDeficit = ResolveCoverageDeficit(livingConditionsState?.PowerCoverageIndex ?? 1m);
            double waterDeficit = ResolveCoverageDeficit(livingConditionsState?.WaterCoverageIndex ?? 1m);
            double heatingDeficit = ResolveCoverageDeficit(livingConditionsState?.HeatingCoverageIndex ?? 1m);
            double sanitationDeficit = ResolveCoverageDeficit(livingConditionsState?.SanitationCoverageIndex ?? 1m);
            double floodingPressure = ResolvePressure(livingConditionsState?.FloodingIndex ?? 0m);
            double continuityDeficit = ResolveCoverageDeficit(livingConditionsState?.UtilityContinuityIndex ?? 1m);

            double foodShortage = ResolvePressure(essentialsState?.FoodShortageRiskIndex ?? 0m);
            double medicineShortage = ResolvePressure(essentialsState?.MedicineShortageRiskIndex ?? 0m);
            double emergencyWaterShortage = ResolvePressure(essentialsState?.EmergencyWaterShortageRiskIndex ?? 0m);
            double rationingPressure = essentialsState?.EmergencyRationingEnabled == true ? 0.18d : 0d;

            double energyPenalty = ResolveLowValuePenalty(person.Energy.Value, 35d);
            double healthPenalty = ResolveLowValuePenalty(person.Health.Value, 45d);
            double happinessPenalty = ResolveLowValuePenalty(person.Happiness.Value, 35d);
            double stressPenalty = ResolveHighValuePenalty(person.Stress.Value, 55d);
            double illnessAttendancePenalty = ResolveIllnessAttendancePenalty(person);
            double illnessProductivityPenalty = ResolveIllnessProductivityPenalty(person);
            double housingPenalty = housingStatus == HousingStatus.Homeless ? 0.12d : 0d;
            double agePenalty = person.GetAgeGroup(currentDate) == AgeGroup.Senior ? 0.05d : 0d;

            double attendance = 1d -
                                (roadDeficit * 0.32d) -
                                (floodingPressure * 0.22d) -
                                (powerDeficit * 0.10d) -
                                (waterDeficit * 0.08d) -
                                (heatingDeficit * 0.06d) -
                                (foodShortage * 0.08d) -
                                (emergencyWaterShortage * 0.06d) -
                                (rationingPressure * 0.05d) -
                                (energyPenalty * 0.22d) -
                                (stressPenalty * 0.16d) -
                                (healthPenalty * 0.18d) -
                                illnessAttendancePenalty -
                                housingPenalty -
                                agePenalty;

            double productivity = 1d -
                                  (powerDeficit * 0.16d) -
                                  (waterDeficit * 0.08d) -
                                  (sanitationDeficit * 0.05d) -
                                  (continuityDeficit * 0.10d) -
                                  (foodShortage * 0.16d) -
                                  (medicineShortage * 0.07d) -
                                  (emergencyWaterShortage * 0.05d) -
                                  (energyPenalty * 0.28d) -
                                  (stressPenalty * 0.22d) -
                                  (healthPenalty * 0.18d) -
                                  (happinessPenalty * 0.10d) -
                                  illnessProductivityPenalty -
                                  (housingPenalty * 0.50d);

            decimal attendanceIndex = RoundIndex(Math.Clamp(attendance, 0.20d, 1d));
            decimal productivityIndex = RoundIndex(Math.Clamp(productivity, 0.25d, 1d));
            decimal payrollMultiplier = RoundIndex(Math.Clamp(
                (double)attendanceIndex * 0.60d + (double)productivityIndex * 0.40d,
                0.25d,
                1d));

            return new CityPopulationParticipationProfile(
                AttendanceIndex: attendanceIndex,
                ProductivityIndex: productivityIndex,
                PayrollMultiplier: payrollMultiplier);
        }

        public decimal ResolveStudentAttendanceIndex(
            Person person,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            CityPopulationLivingConditionsState? livingConditionsState,
            CityPopulationEssentialsState? essentialsState)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (!person.IsAlive || person.Employment.Status != EmploymentStatus.Student)
                return 1m;

            double roadDeficit = ResolveCoverageDeficit(livingConditionsState?.RoadAccessibilityIndex ?? 1m);
            double powerDeficit = ResolveCoverageDeficit(livingConditionsState?.PowerCoverageIndex ?? 1m);
            double waterDeficit = ResolveCoverageDeficit(livingConditionsState?.WaterCoverageIndex ?? 1m);
            double heatingDeficit = ResolveCoverageDeficit(livingConditionsState?.HeatingCoverageIndex ?? 1m);
            double floodingPressure = ResolvePressure(livingConditionsState?.FloodingIndex ?? 0m);
            double foodShortage = ResolvePressure(essentialsState?.FoodShortageRiskIndex ?? 0m);
            double emergencyWaterShortage = ResolvePressure(essentialsState?.EmergencyWaterShortageRiskIndex ?? 0m);
            double rationingPressure = essentialsState?.EmergencyRationingEnabled == true ? 0.15d : 0d;

            double energyPenalty = ResolveLowValuePenalty(person.Energy.Value, 35d);
            double healthPenalty = ResolveLowValuePenalty(person.Health.Value, 45d);
            double stressPenalty = ResolveHighValuePenalty(person.Stress.Value, 50d);
            double illnessAttendancePenalty = ResolveIllnessAttendancePenalty(person);
            double housingPenalty = housingStatus == HousingStatus.Homeless ? 0.12d : 0d;
            double agePenalty = person.GetAgeGroup(currentDate) == AgeGroup.Child ? -0.03d : 0d;

            double attendance = 1d -
                                (roadDeficit * 0.30d) -
                                (floodingPressure * 0.20d) -
                                (powerDeficit * 0.16d) -
                                (waterDeficit * 0.09d) -
                                (heatingDeficit * 0.08d) -
                                (foodShortage * 0.10d) -
                                (emergencyWaterShortage * 0.06d) -
                                (rationingPressure * 0.05d) -
                                (energyPenalty * 0.24d) -
                                (stressPenalty * 0.18d) -
                                (healthPenalty * 0.14d) -
                                illnessAttendancePenalty -
                                housingPenalty -
                                agePenalty;

            return RoundIndex(Math.Clamp(attendance, 0.18d, 1d));
        }

        private static double ResolveCoverageDeficit(decimal value)
        {
            return Math.Clamp((double)(1m - value), 0d, 1.50d);
        }

        private static double ResolvePressure(decimal value)
        {
            return Math.Clamp((double)value, 0d, 1.50d);
        }

        private static double ResolveLowValuePenalty(int currentValue, double healthyThreshold)
        {
            return currentValue >= healthyThreshold
                ? 0d
                : Math.Clamp((healthyThreshold - currentValue) / healthyThreshold, 0d, 1d);
        }

        private static double ResolveHighValuePenalty(int currentValue, double toleranceThreshold)
        {
            return currentValue <= toleranceThreshold
                ? 0d
                : Math.Clamp((currentValue - toleranceThreshold) / (100d - toleranceThreshold), 0d, 1d);
        }

        private static double ResolveIllnessAttendancePenalty(Person person)
        {
            return person.CurrentIllnessSeverity switch
            {
                IllnessSeverity.Mild => 0.08d,
                IllnessSeverity.Moderate => 0.18d,
                IllnessSeverity.Severe => 0.35d,
                _ => 0d
            };
        }

        private static double ResolveIllnessProductivityPenalty(Person person)
        {
            return person.CurrentIllnessSeverity switch
            {
                IllnessSeverity.Mild => 0.10d,
                IllnessSeverity.Moderate => 0.22d,
                IllnessSeverity.Severe => 0.40d,
                _ => 0d
            };
        }

        private static decimal RoundIndex(double value)
        {
            return decimal.Round(
                d: (decimal)value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
