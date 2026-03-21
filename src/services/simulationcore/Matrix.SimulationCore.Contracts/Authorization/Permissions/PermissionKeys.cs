namespace Matrix.SimulationCore.Contracts.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string CityCoreScenariosCatalogRead = "citycore.scenarios.catalog.read";

        public const string CityCoreClassicCityRead = "citycore.classic-city.read";
        public const string CityCoreClassicCityCreate = "citycore.classic-city.create";
        public const string CityCoreClassicCityUpdate = "citycore.classic-city.update";
        public const string CityCoreClassicCityArchive = "citycore.classic-city.archive";
        public const string CityCoreClassicCityDelete = "citycore.classic-city.delete";

        public const string CityCoreClassicCityPopulationBootstrapRetry =
            "citycore.classic-city.population-bootstrap.retry";

        public const string CityCoreSimulationRead = "citycore.simulations.read";
        public const string CityCoreSimulationControl = "citycore.simulations.control";
    }
}
