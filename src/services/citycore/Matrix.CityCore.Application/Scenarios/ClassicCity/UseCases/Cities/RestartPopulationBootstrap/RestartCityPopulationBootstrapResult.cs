namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
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
        string? SimulationKind)
    {
        public static RestartCityPopulationBootstrapResult Restarted(
            Guid operationId,
            string simulationKind)
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.Restarted,
                PopulationBootstrapOperationId: operationId,
                SimulationKind: simulationKind);
        }

        public static RestartCityPopulationBootstrapResult NotFound()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotFound,
                PopulationBootstrapOperationId: null,
                SimulationKind: null);
        }

        public static RestartCityPopulationBootstrapResult NotAllowed()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotAllowed,
                PopulationBootstrapOperationId: null,
                SimulationKind: null);
        }
    }
}
