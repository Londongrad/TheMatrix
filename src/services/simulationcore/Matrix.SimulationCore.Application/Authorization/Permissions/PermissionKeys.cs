namespace Matrix.SimulationCore.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string SimulationCoreScenariosCatalogRead = "simulationcore.scenarios.catalog.read";

        public const string SimulationCoreClassicCityRead = "simulationcore.classic-city.read";
        public const string SimulationCoreClassicCityCreate = "simulationcore.classic-city.create";
        public const string SimulationCoreClassicCityUpdate = "simulationcore.classic-city.update";
        public const string SimulationCoreClassicCityArchive = "simulationcore.classic-city.archive";
        public const string SimulationCoreClassicCityDelete = "simulationcore.classic-city.delete";

        public const string SimulationCoreClassicCityPopulationBootstrapRetry =
            "simulationcore.classic-city.population-bootstrap.retry";

        public const string SimulationCoreSimulationRead = "simulationcore.simulations.read";
        public const string SimulationCoreSimulationControl = "simulationcore.simulations.control";
    }
}
