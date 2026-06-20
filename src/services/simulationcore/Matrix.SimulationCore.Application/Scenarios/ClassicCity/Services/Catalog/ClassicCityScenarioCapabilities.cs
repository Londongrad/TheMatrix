namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Catalog
{
    public static class ClassicCityScenarioCapabilities
    {
        public const string Provisioning = "provisioning";
        public const string Topology = "topology";
        public const string Routing = "routing";
        public const string Trips = "trips";
        public const string Weather = "weather";
        public const string Population = "population";
        public const string Economy = "economy";
        public const string Resources = "resources";
        public const string InfrastructureSystems = "infrastructure-systems";

        public static IReadOnlyList<string> All { get; } =
        [
            Provisioning,
            Topology,
            Routing,
            Trips,
            Weather,
            Population,
            Economy,
            Resources,
            InfrastructureSystems
        ];
    }
}
