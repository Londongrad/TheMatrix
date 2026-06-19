namespace Matrix.SimulationCore.Contracts.Events
{
    public static class SimulationCoreEventTypes
    {
        public const string SimulationCreatedV1 = "simulationcore.simulation-created.v1";
        public const string SimulationArchivedV1 = "simulationcore.simulation-archived.v1";
        public const string SimulationDeletedV1 = "simulationcore.simulation-deleted.v1";
        public const string SimulationTickPhaseReachedV1 = "simulationcore.simulation-tick-phase-reached.v1";
        public const string WeatherOverrideStartedV1 = "simulationcore.weather-override-started.v1";
        public const string WeatherOverrideCancelledV1 = "simulationcore.weather-override-cancelled.v1";
        public const string WeatherOverrideExpiredV1 = "simulationcore.weather-override-expired.v1";
        public const string ClimateProfileChangedV1 = "simulationcore.climate-profile-changed.v1";
    }
}
