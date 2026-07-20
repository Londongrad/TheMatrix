namespace Matrix.Population.Application.Integration
{
    public sealed record PopulationResidentHealthcareAccessSnapshot(
        bool HasPrimaryCareDestination,
        bool IsPrimaryCareInCommunity,
        bool HasRouteData,
        bool IsRouteAccessible,
        double RouteAccessibilityIndex,
        double RoutePassabilityIndex,
        double? EstimatedTravelTimeMinutes,
        bool HasInfrastructureData,
        double UtilityIncidentDispatchReadinessIndex,
        double UtilityIncidentPressureIndex,
        double UtilityIncidentCoordinationDifficultyIndex,
        double UtilityIncidentRestorationPriorityIndex,
        double PowerCoverageIndex,
        double WaterCoverageIndex,
        double HeatingCoverageIndex,
        double SanitationCoverageIndex,
        double HealthcareQualityIndex,
        double RecoverySupportIndex,
        double TriagePressureIndex);
}
