using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
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
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials,
            CityPopulationCommuteContext? commute = null)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (!person.IsAlive || person.Employment.Status != EmploymentStatus.Employed)
                return CityPopulationParticipationProfile.Full;

            double roadDeficit = ResolveCoverageDeficit(livingConditions.RoadAccessibilityIndex);
            double powerDeficit = ResolveCoverageDeficit(livingConditions.PowerCoverageIndex);
            double waterDeficit = ResolveCoverageDeficit(livingConditions.WaterCoverageIndex);
            double heatingDeficit = ResolveCoverageDeficit(livingConditions.HeatingCoverageIndex);
            double sanitationDeficit = ResolveCoverageDeficit(livingConditions.SanitationCoverageIndex);
            double floodingPressure = ResolvePressure(livingConditions.FloodingIndex);
            double continuityDeficit = ResolveCoverageDeficit(livingConditions.UtilityContinuityIndex);

            double foodShortage = ResolvePressure(essentials.FoodShortageRiskIndex);
            double medicineShortage = ResolvePressure(essentials.MedicineShortageRiskIndex);
            double emergencyWaterShortage = ResolvePressure(essentials.EmergencyWaterShortageRiskIndex);
            double rationingPressure = essentials.EmergencyRationingEnabled
                ? 0.18d
                : 0d;

            double energyPenalty = ResolveLowValuePenalty(
                currentValue: person.Energy.Value,
                healthyThreshold: 35d);
            double functionalCapacityDeficit = ResolveFunctionalCapacityDeficit(person);
            double happinessPenalty = ResolveLowValuePenalty(
                currentValue: person.Happiness.Value,
                healthyThreshold: 35d);
            double stressPenalty = ResolveHighValuePenalty(
                currentValue: person.Stress.Value,
                toleranceThreshold: 55d);
            double housingPenalty = housingStatus == HousingStatus.Homeless
                ? 0.12d
                : 0d;
            double agePenalty = person.GetAgeGroup(currentDate) == AgeGroup.Senior
                ? 0.05d
                : 0d;
            double commutePenalty = ResolveCommutePenalty(commute);
            double commuteAccessPenalty = ResolveCommuteAccessPenalty(commute);

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
                                (functionalCapacityDeficit * 0.50d) -
                                (commutePenalty * 0.30d) -
                                commuteAccessPenalty -
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
                                  (functionalCapacityDeficit * 0.55d) -
                                  (happinessPenalty * 0.10d) -
                                  (commutePenalty * 0.12d) -
                                  (commuteAccessPenalty * 0.35d) -
                                  (housingPenalty * 0.50d);

            decimal attendanceIndex = RoundIndex(
                Math.Clamp(
                    value: attendance,
                    min: 0.20d,
                    max: 1d));
            decimal productivityIndex = RoundIndex(
                Math.Clamp(
                    value: productivity,
                    min: 0.25d,
                    max: 1d));
            decimal payrollMultiplier = RoundIndex(
                Math.Clamp(
                    value: ((double)attendanceIndex * 0.60d) + ((double)productivityIndex * 0.40d),
                    min: 0.25d,
                    max: 1d));

            return new CityPopulationParticipationProfile(
                AttendanceIndex: attendanceIndex,
                ProductivityIndex: productivityIndex,
                PayrollMultiplier: payrollMultiplier);
        }

        public decimal ResolveStudentAttendanceIndex(
            Person person,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            CityPopulationLivingConditionsContext livingConditions,
            CityPopulationEssentialsContext essentials,
            CityPopulationCommuteContext? commute = null)
        {
            ArgumentNullException.ThrowIfNull(person);

            if (!person.IsAlive || person.Employment.Status != EmploymentStatus.Student)
                return 1m;

            double roadDeficit = ResolveCoverageDeficit(livingConditions.RoadAccessibilityIndex);
            double powerDeficit = ResolveCoverageDeficit(livingConditions.PowerCoverageIndex);
            double waterDeficit = ResolveCoverageDeficit(livingConditions.WaterCoverageIndex);
            double heatingDeficit = ResolveCoverageDeficit(livingConditions.HeatingCoverageIndex);
            double floodingPressure = ResolvePressure(livingConditions.FloodingIndex);
            double foodShortage = ResolvePressure(essentials.FoodShortageRiskIndex);
            double emergencyWaterShortage = ResolvePressure(essentials.EmergencyWaterShortageRiskIndex);
            double rationingPressure = essentials.EmergencyRationingEnabled
                ? 0.15d
                : 0d;

            double energyPenalty = ResolveLowValuePenalty(
                currentValue: person.Energy.Value,
                healthyThreshold: 35d);
            double functionalCapacityDeficit = ResolveFunctionalCapacityDeficit(person);
            double stressPenalty = ResolveHighValuePenalty(
                currentValue: person.Stress.Value,
                toleranceThreshold: 50d);
            double housingPenalty = housingStatus == HousingStatus.Homeless
                ? 0.12d
                : 0d;
            double agePenalty = person.GetAgeGroup(currentDate) == AgeGroup.Child
                ? -0.03d
                : 0d;
            double commutePenalty = ResolveCommutePenalty(commute);
            double commuteAccessPenalty = ResolveCommuteAccessPenalty(commute);

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
                                (functionalCapacityDeficit * 0.45d) -
                                (commutePenalty * 0.34d) -
                                commuteAccessPenalty -
                                housingPenalty -
                                agePenalty;

            return RoundIndex(
                Math.Clamp(
                    value: attendance,
                    min: 0.18d,
                    max: 1d));
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

        private static double ResolveLowValuePenalty(
            int currentValue,
            double healthyThreshold)
        {
            return currentValue >= healthyThreshold
                ? 0d
                : Math.Clamp(
                    value: (healthyThreshold - currentValue) / healthyThreshold,
                    min: 0d,
                    max: 1d);
        }

        private static double ResolveHighValuePenalty(
            int currentValue,
            double toleranceThreshold)
        {
            return currentValue <= toleranceThreshold
                ? 0d
                : Math.Clamp(
                    value: (currentValue - toleranceThreshold) / (100d - toleranceThreshold),
                    min: 0d,
                    max: 1d);
        }

        private static double ResolveFunctionalCapacityDeficit(Person person)
        {
            return Math.Clamp(
                value: (100d - person.FunctionalCapacity.Value) / 100d,
                min: 0d,
                max: 1d);
        }

        private static decimal RoundIndex(double value)
        {
            return decimal.Round(
                d: (decimal)value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static double ResolveCommutePenalty(CityPopulationCommuteContext? commute)
        {
            if (commute is null || !commute.HasRouteData)
                return 0d;

            return Math.Clamp(
                value: (double)(1m - commute.AccessibilityIndex),
                min: 0d,
                max: 1d);
        }

        private static double ResolveCommuteAccessPenalty(CityPopulationCommuteContext? commute)
        {
            return commute is
            {
                HasRouteData: true, IsAccessible: false
            }
                ? 0.22d
                : 0d;
        }
    }
}
