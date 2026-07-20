namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientEnvironmentalHealthPolicy
    {
        public double ResolvePublicHealthRiskStrength(PatientEnvironmentalHealthContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            double waterDeficit = ResolveCoverageDeficit(
                context.WaterCoverageIndex,
                nameof(context.WaterCoverageIndex));
            double sanitationDeficit = ResolveCoverageDeficit(
                context.SanitationCoverageIndex,
                nameof(context.SanitationCoverageIndex));
            double floodingPressure = ResolvePressure(
                context.FloodingIndex,
                nameof(context.FloodingIndex));
            double emergencyWaterShortage = ResolvePressure(
                context.EmergencyWaterShortageRiskIndex,
                nameof(context.EmergencyWaterShortageRiskIndex));
            double foodShortage = ResolvePressure(
                                      context.FoodShortageRiskIndex,
                                      nameof(context.FoodShortageRiskIndex)) *
                                  0.35d;

            double blended = (waterDeficit * 0.28d) +
                             (sanitationDeficit * 0.28d) +
                             (floodingPressure * 0.22d) +
                             (emergencyWaterShortage * 0.17d) +
                             foodShortage;

            return Math.Clamp(blended, 0d, 1d);
        }

        public double ResolveMedicineAccessStrength(PatientEnvironmentalHealthContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            double medicineShortage = ResolvePressure(
                context.MedicineShortageRiskIndex,
                nameof(context.MedicineShortageRiskIndex));
            double continuityDeficit = ResolveCoverageDeficit(
                context.UtilityContinuityIndex,
                nameof(context.UtilityContinuityIndex));
            double access = 1d -
                            (medicineShortage * 0.75d) -
                            (continuityDeficit * 0.15d);

            if (context.EmergencyRationingEnabled)
                access -= 0.05d;

            return Math.Clamp(access, 0.25d, 1d);
        }

        private static double ResolveCoverageDeficit(
            double value,
            string parameterName)
        {
            EnsureFinite(value, parameterName);
            return Math.Clamp(1d - value, 0d, 1.50d);
        }

        private static double ResolvePressure(
            double value,
            string parameterName)
        {
            EnsureFinite(value, parameterName);
            return Math.Clamp(value, 0d, 1.50d);
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
