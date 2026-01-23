using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Simulation
{
    internal sealed class SimulationApiClient(HttpClient client) : ISimulationApiClient
    {
        private const string SimulationSegment = "/simulation";
        private const string CitiesEndpoint = "/api/cities";

        private const string ClockPauseEndpoint = "/pause";
        private const string ClockResumeEndpoint = "/resume";
        private const string ClockSpeedEndpoint = "/speed";
        private const string ClockJumpEndpoint = "/jump";
        private readonly HttpClient _client = client;

        public async Task<SimulationClockView> GetClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string requestUri = $"{CitiesEndpoint}/{cityId}{SimulationSegment}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: requestUri,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<SimulationClockView>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: requestUri);
        }

        public async Task PauseClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{SimulationSegment}{ClockPauseEndpoint}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task ResumeClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{SimulationSegment}{ClockResumeEndpoint}",
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task SetClockSpeedAsync(
            Guid cityId,
            SetSpeedRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{SimulationSegment}{ClockSpeedEndpoint}",
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }

        public async Task JumpClockAsync(
            Guid cityId,
            JumpClockRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{SimulationSegment}{ClockJumpEndpoint}",
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }
    }
}
