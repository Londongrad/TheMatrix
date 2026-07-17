using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityHealthcareAutonomyPolicy(CityHouseholdLivelihoodPolicy householdLivelihoodPolicy)
    {
        public double ResolveSupportStrength(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            HousingStatus? housingStatus,
            DateOnly currentDate,
            bool hasPrimaryCareAccess,
            bool hasDistrictPrimaryCareAccess,
            CityDistrictUtilityConditionsSnapshot? districtUtilityConditions = null,
            CityPopulationCommuteContext? healthcareCommute = null,
            CityPopulationServiceQualityState? serviceQualityState = null,
            CityPopulationHealthcarePressureProfile? healthcarePressureProfile = null)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(householdResidents);

            if (!resident.IsAlive)
                return 0d;

            CityHouseholdLivelihoodProfile livelihood = householdLivelihoodPolicy.Build(
                householdResidents: householdResidents,
                housingStatus: housingStatus,
                currentDate: currentDate);

            double access = 0.02d +
                            (livelihood.StabilityScore * 0.12d) +
                            (livelihood.IsHoused
                                ? 0.04d
                                : 0d) +
                            (livelihood.AdultProviderCount * 0.03d) +
                            (livelihood.AdultStructuredParticipantCount * 0.01d) -
                            (livelihood.FunctionalLimitationCount > 1
                                ? 0.03d
                                : 0d);

            if (resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior)
                access += 0.03d;

            if (HasSevereFunctionalLimitation(resident))
                access += 0.03d;

            if (resident.Employment.Status == EmploymentStatus.Employed)
                access += 0.02d;

            if (hasPrimaryCareAccess)
                access += hasDistrictPrimaryCareAccess
                    ? 0.05d
                    : 0.02d;

            if (healthcareCommute is not null)
            {
                double accessibility = Math.Clamp(
                    value: (double)healthcareCommute.AccessibilityIndex,
                    min: 0d,
                    max: 1d);
                double passability = Math.Clamp(
                    value: (double)healthcareCommute.PassabilityIndex,
                    min: 0d,
                    max: 1d);

                access *= 0.55d + (accessibility * 0.45d);
                access *= 0.75d + (passability * 0.25d);

                if (!healthcareCommute.IsAccessible)
                    access *= 0.45d;

                if (healthcareCommute.EstimatedTravelTimeMinutes.HasValue)
                {
                    double travelTimeMinutes = (double)healthcareCommute.EstimatedTravelTimeMinutes.Value;
                    if (travelTimeMinutes >= 90d)
                        access *= 0.75d;
                    else
                        if (travelTimeMinutes >= 45d)
                        access *= 0.88d;
                }
            }

            if (!livelihood.HasStructuredSupport)
                access *= 0.60d;

            if (districtUtilityConditions is not null)
            {
                double districtMedicalAccessStability =
                    ResolveDistrictMedicalAccessStability(districtUtilityConditions);
                access *= 0.70d + (districtMedicalAccessStability * 0.35d);

                if (HasSevereFunctionalLimitation(resident) &&
                    districtMedicalAccessStability < 0.40d)
                    access *= 0.88d;
            }

            decimal healthcareQualityIndex = serviceQualityState?.HealthcareQualityIndex ?? 1m;
            access += (double)((healthcareQualityIndex - 1m) * 0.14m);

            if (healthcareQualityIndex < 0.85m)
                access *= 0.92d;

            if (healthcarePressureProfile is not null)
            {
                double recoverySupportMultiplier = Math.Clamp(
                    value: (double)healthcarePressureProfile.RecoverySupportIndex,
                    min: 0.45d,
                    max: 1.35d);
                double triagePressure = Math.Clamp(
                    value: (double)(healthcarePressureProfile.TriagePressureIndex / 3m),
                    min: 0d,
                    max: 1d);

                access *= recoverySupportMultiplier;

                if (HasSevereFunctionalLimitation(resident))
                    access += triagePressure * 0.05d;
                else
                    if (HasModerateFunctionalLimitation(resident))
                    access -= triagePressure * 0.01d;
                else
                    access -= triagePressure * 0.04d;
            }

            return Math.Clamp(
                value: access,
                min: 0d,
                max: 0.48d);
        }

        private static bool HasSevereFunctionalLimitation(Person resident) =>
            resident.FunctionalCapacity.Value < 50;

        private static bool HasModerateFunctionalLimitation(Person resident) =>
            resident.FunctionalCapacity.Value is >= 50 and < 80;

        private static double ResolveDistrictMedicalAccessStability(
            CityDistrictUtilityConditionsSnapshot districtUtilityConditions)
        {
            double stability =
                ((double)districtUtilityConditions.UtilityIncidentDispatchReadinessIndex * 0.28d) +
                ((double)(1m - districtUtilityConditions.UtilityIncidentPressureIndex) * 0.24d) +
                ((double)(1m - districtUtilityConditions.UtilityIncidentCoordinationDifficultyIndex) * 0.16d) +
                ((double)(1m - districtUtilityConditions.UtilityIncidentRestorationPriorityIndex) * 0.12d) +
                ((double)districtUtilityConditions.PowerCoverageIndex * 0.08d) +
                ((double)districtUtilityConditions.WaterCoverageIndex * 0.07d) +
                ((double)districtUtilityConditions.HeatingCoverageIndex * 0.03d) +
                ((double)districtUtilityConditions.SanitationCoverageIndex * 0.02d);

            return Math.Clamp(
                value: stability,
                min: 0d,
                max: 1d);
        }
    }
}
