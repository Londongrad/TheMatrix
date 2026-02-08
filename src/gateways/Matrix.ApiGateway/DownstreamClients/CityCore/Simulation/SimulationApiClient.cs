using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Simulation
{
    internal sealed class SimulationApiClient(HttpClient client) : ISimulationApiClient
    {
        private const string SimulationsEndpoint = "/api/simulations";
        private const string ClockPauseEndpoint = "/pause";
        private const string ClockResumeEndpoint = "/resume";
        private const string ClockSpeedEndpoint = "/speed";
        private const string ClockJumpEndpoint = "/jump";
        private readonly HttpClient _client = client;

        public async Task<SimulationClockView> GetClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default)
        {
            string requestUri = $"{SimulationsEndpoint}/{simulationId}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: requestUri,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<SimulationClockView>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: requestUri);
        }

        public async Task PauseClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{SimulationsEndpoint}/{simulationId}{ClockPauseEndpoint}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task ResumeClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{SimulationsEndpoint}/{simulationId}{ClockResumeEndpoint}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task SetClockSpeedAsync(
            Guid simulationId,
            SetSpeedRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: $"{SimulationsEndpoint}/{simulationId}{ClockSpeedEndpoint}",
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task JumpClockAsync(
            Guid simulationId,
            JumpClockRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: $"{SimulationsEndpoint}/{simulationId}{ClockJumpEndpoint}",
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }
    }
}
