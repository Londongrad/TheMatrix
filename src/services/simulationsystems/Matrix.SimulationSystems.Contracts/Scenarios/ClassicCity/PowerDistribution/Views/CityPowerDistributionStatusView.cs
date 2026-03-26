namespace Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views
{
    public sealed record CityPowerDistributionStatusView(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        bool EmergencyModeEnabled,
        decimal SubstationCapacityIndex,
        decimal GridIntegrityIndex,
        decimal SwitchingReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityPowerDistributionSystemStatusView System);
}
