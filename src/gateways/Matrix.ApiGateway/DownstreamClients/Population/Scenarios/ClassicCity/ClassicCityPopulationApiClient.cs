using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient(HttpClient client)
        : IClassicCityPopulationApiClient
    {
        private const string ServiceName = DownstreamServiceNames.Population;
        private const string PopulationBaseEndpoint = ClassicCityPopulationApiRoutes.PopulationPath;
        private const string InitializeEndpoint = PopulationBaseEndpoint + "/init";
        private readonly HttpClient _client = client;
    }
}
