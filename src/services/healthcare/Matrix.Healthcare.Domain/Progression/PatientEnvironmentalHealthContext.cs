namespace Matrix.Healthcare.Domain.Progression
{
    public sealed record PatientEnvironmentalHealthContext(
        double WaterCoverageIndex,
        double SanitationCoverageIndex,
        double FloodingIndex,
        double UtilityContinuityIndex,
        double EmergencyWaterShortageRiskIndex,
        double FoodShortageRiskIndex,
        double MedicineShortageRiskIndex,
        bool EmergencyRationingEnabled);
}
