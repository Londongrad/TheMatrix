namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public static class ClassicCityOutboxEventTypes
    {
        public const string ClassicCityOperationalExpenseIncurredV1 =
            "simulation-systems.classic-city-operational-expense-incurred.v1";

        public const string ClassicCityLivingConditionsSnapshotV1 =
            "simulation-systems.classic-city-living-conditions-snapshot.v1";

        public const string ClassicCitySystemsResourceDemandSnapshotV1 =
            "simulation-systems.classic-city-systems-resource-demand-snapshot.v1";
    }
}
