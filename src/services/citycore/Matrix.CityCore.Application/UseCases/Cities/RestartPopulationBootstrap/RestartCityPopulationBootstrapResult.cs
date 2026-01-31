namespace Matrix.CityCore.Application.UseCases.Cities.RestartPopulationBootstrap
{
    public enum RestartCityPopulationBootstrapStatus
    {
        Restarted = 1,
        NotFound = 2,
        NotAllowed = 3
    }

    public sealed record RestartCityPopulationBootstrapResult(
        RestartCityPopulationBootstrapStatus Status,
        Guid? PopulationBootstrapOperationId)
    {
        public static RestartCityPopulationBootstrapResult Restarted(Guid operationId)
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.Restarted,
                PopulationBootstrapOperationId: operationId);
        }

        public static RestartCityPopulationBootstrapResult NotFound()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotFound,
                PopulationBootstrapOperationId: null);
        }

        public static RestartCityPopulationBootstrapResult NotAllowed()
        {
            return new RestartCityPopulationBootstrapResult(
                Status: RestartCityPopulationBootstrapStatus.NotAllowed,
                PopulationBootstrapOperationId: null);
        }
    }
}
