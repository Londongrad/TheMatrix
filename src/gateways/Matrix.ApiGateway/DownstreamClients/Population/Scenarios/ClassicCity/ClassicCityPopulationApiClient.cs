using Matrix.ApiGateway.DownstreamClients.Common;

namespace Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity
{
    internal sealed partial class ClassicCityPopulationApiClient(HttpClient client)
    {
        private const string ServiceName = DownstreamServiceNames.Population;
        private const string PopulationBaseEndpoint = "/api/population";
        private const string InitializeEndpoint = PopulationBaseEndpoint + "/init";
        private readonly HttpClient _client = client;
    }
}
