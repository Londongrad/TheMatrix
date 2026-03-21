namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public enum RestartCityPopulationBootstrapStatus
    {
        Restarted = 1,
        NotFound = 2,
        NotAllowed = 3
    }

    public sealed record RestartCityPopulationBootstrapResult(
        RestartCityPopulationBootstrapStatus Status,
        Guid? PopulationBootstrapOperationId,
        Guid? EconomyBootstrapOperationId,
        string? SimulationKind)
    {
        public static RestartCityPopulationBootstrapResult Restarted(
            Guid populationOperationId,
            Guid economyOperationId,
            string simulationKind)
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.Restarted,
                PopulationBootstrapOperationId: populationOperationId,
                EconomyBootstrapOperationId: economyOperationId,
                SimulationKind: simulationKind);
        }

        public static RestartCityPopulationBootstrapResult NotFound()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotFound,
                PopulationBootstrapOperationId: null,
                EconomyBootstrapOperationId: null,
                SimulationKind: null);
        }

        public static RestartCityPopulationBootstrapResult NotAllowed()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotAllowed,
                PopulationBootstrapOperationId: null,
                EconomyBootstrapOperationId: null,
                SimulationKind: null);
        }
    }
}
