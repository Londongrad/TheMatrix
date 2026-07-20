namespace Matrix.Population.Application.Integration
{
    public sealed record PopulationResidentEnvironmentalHealthSnapshot(
        double WaterCoverageIndex,
        double SanitationCoverageIndex,
        double FloodingIndex,
        double UtilityContinuityIndex,
        double EmergencyWaterShortageRiskIndex,
        double FoodShortageRiskIndex,
        double MedicineShortageRiskIndex,
        bool EmergencyRationingEnabled);
}
