namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentEnvironmentalHealthContextV1(
        double WaterCoverageIndex,
        double SanitationCoverageIndex,
        double FloodingIndex,
        double UtilityContinuityIndex,
        double EmergencyWaterShortageRiskIndex,
        double FoodShortageRiskIndex,
        double MedicineShortageRiskIndex,
        bool EmergencyRationingEnabled);
}
