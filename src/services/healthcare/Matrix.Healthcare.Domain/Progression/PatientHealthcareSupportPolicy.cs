namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientHealthcareSupportPolicy(
        PatientEnvironmentalHealthPolicy environmentalHealthPolicy)
    {
        public double ResolveSupportStrength(
            int functionalCapacityScore,
            bool isVulnerable,
            bool isEmployed,
            PatientHousingStability housingStability,
            PatientHouseholdHealthContext household,
            PatientHealthcareAccessContext healthcareAccess,
            PatientEnvironmentalHealthContext environment)
        {
            if (functionalCapacityScore is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(functionalCapacityScore));
            if (!Enum.IsDefined(housingStability))
                throw new ArgumentOutOfRangeException(nameof(housingStability));

            Validate(household);
            Validate(healthcareAccess);
            ArgumentNullException.ThrowIfNull(environment);

            double access = 0.02d +
                            (household.StabilityScore * 0.12d) +
                            (housingStability == PatientHousingStability.Housed
                                ? 0.04d
                                : 0d) +
                            (household.AdultProviderCount * 0.03d) +
                            (household.AdultStructuredParticipantCount * 0.01d) -
                            (household.FunctionalLimitationCount > 1
                                ? 0.03d
                                : 0d);

            if (isVulnerable)
                access += 0.03d;
            if (functionalCapacityScore < 50)
                access += 0.03d;
            if (isEmployed)
                access += 0.02d;
            if (healthcareAccess.HasPrimaryCareDestination)
                access += healthcareAccess.IsPrimaryCareInCommunity
                    ? 0.05d
                    : 0.02d;

            if (healthcareAccess.HasRouteData)
            {
                double accessibility = Math.Clamp(
                    healthcareAccess.RouteAccessibilityIndex,
                    0d,
                    1d);
                double passability = Math.Clamp(
                    healthcareAccess.RoutePassabilityIndex,
                    0d,
                    1d);

                access *= 0.55d + (accessibility * 0.45d);
                access *= 0.75d + (passability * 0.25d);

                if (!healthcareAccess.IsRouteAccessible)
                    access *= 0.45d;

                if (healthcareAccess.EstimatedTravelTimeMinutes >= 90d)
                    access *= 0.75d;
                else if (healthcareAccess.EstimatedTravelTimeMinutes >= 45d)
                    access *= 0.88d;
            }

            if (!household.HasStructuredSupport)
                access *= 0.60d;

            if (healthcareAccess.HasInfrastructureData)
            {
                double infrastructureStability = ResolveInfrastructureStability(healthcareAccess);
                access *= 0.70d + (infrastructureStability * 0.35d);

                if (functionalCapacityScore < 50 && infrastructureStability < 0.40d)
                    access *= 0.88d;
            }

            access += (healthcareAccess.HealthcareQualityIndex - 1d) * 0.14d;

            if (healthcareAccess.HealthcareQualityIndex < 0.85d)
                access *= 0.92d;

            double recoverySupportMultiplier = Math.Clamp(
                healthcareAccess.RecoverySupportIndex,
                0.45d,
                1.35d);
            double triagePressure = Math.Clamp(
                healthcareAccess.TriagePressureIndex / 3d,
                0d,
                1d);

            access *= recoverySupportMultiplier;

            if (functionalCapacityScore < 50)
                access += triagePressure * 0.05d;
            else if (functionalCapacityScore < 80)
                access -= triagePressure * 0.01d;
            else
                access -= triagePressure * 0.04d;

            double medicineAccess = environmentalHealthPolicy.ResolveMedicineAccessStrength(environment);
            return Math.Clamp(access, 0d, 0.48d) * medicineAccess;
        }

        private static double ResolveInfrastructureStability(PatientHealthcareAccessContext context)
        {
            double stability =
                (context.UtilityIncidentDispatchReadinessIndex * 0.28d) +
                ((1d - context.UtilityIncidentPressureIndex) * 0.24d) +
                ((1d - context.UtilityIncidentCoordinationDifficultyIndex) * 0.16d) +
                ((1d - context.UtilityIncidentRestorationPriorityIndex) * 0.12d) +
                (context.PowerCoverageIndex * 0.08d) +
                (context.WaterCoverageIndex * 0.07d) +
                (context.HeatingCoverageIndex * 0.03d) +
                (context.SanitationCoverageIndex * 0.02d);

            return Math.Clamp(stability, 0d, 1d);
        }

        private static void Validate(PatientHouseholdHealthContext household)
        {
            ArgumentNullException.ThrowIfNull(household);
            EnsureFinite(household.StabilityScore, nameof(household.StabilityScore));
            if (household.AdultProviderCount < 0)
                throw new ArgumentOutOfRangeException(nameof(household.AdultProviderCount));
            if (household.AdultStructuredParticipantCount < 0)
                throw new ArgumentOutOfRangeException(nameof(household.AdultStructuredParticipantCount));
            if (household.FunctionalLimitationCount < 0)
                throw new ArgumentOutOfRangeException(nameof(household.FunctionalLimitationCount));
        }

        private static void Validate(PatientHealthcareAccessContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            double[] values =
            [
                context.RouteAccessibilityIndex,
                context.RoutePassabilityIndex,
                context.UtilityIncidentDispatchReadinessIndex,
                context.UtilityIncidentPressureIndex,
                context.UtilityIncidentCoordinationDifficultyIndex,
                context.UtilityIncidentRestorationPriorityIndex,
                context.PowerCoverageIndex,
                context.WaterCoverageIndex,
                context.HeatingCoverageIndex,
                context.SanitationCoverageIndex,
                context.HealthcareQualityIndex,
                context.RecoverySupportIndex,
                context.TriagePressureIndex
            ];

            foreach (double value in values)
                EnsureFinite(value, nameof(context));

            if (context.EstimatedTravelTimeMinutes.HasValue)
            {
                EnsureFinite(context.EstimatedTravelTimeMinutes.Value, nameof(context));
                if (context.EstimatedTravelTimeMinutes < 0d)
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

        private static void EnsureFinite(
            double value,
            string parameterName)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
