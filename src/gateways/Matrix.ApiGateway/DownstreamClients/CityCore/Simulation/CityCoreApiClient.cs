using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Simulation
{
    internal sealed class CityCoreApiClient(HttpClient client) : ICityCoreApiClient
    {
        private const string CitiesEndpoint = "/api/cities";

        private const string ClockEndpoint = "/clock";
        private const string ClockPauseEndpoint = "/clock/pause";
        private const string ClockResumeEndpoint = "/clock/resume";
        private const string ClockSpeedEndpoint = "/clock/speed";
        private const string ClockJumpEndpoint = "/clock/jump";
        private readonly HttpClient _client = client;

        public async Task<SimulationClockView?> GetClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{ClockEndpoint}",
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<SimulationClockView>(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken,
                requestUrl: $"{CitiesEndpoint}/{cityId}{ClockEndpoint}");
        }

        public async Task PauseClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _client.PostAsync(
                requestUri: $"{CitiesEndpoint}/{cityId}{ClockPauseEndpoint}",
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
                requestUri: $"{CitiesEndpoint}/{cityId}{ClockResumeEndpoint}",
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
                requestUri: $"{CitiesEndpoint}/{cityId}{ClockSpeedEndpoint}",
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
                requestUri: $"{CitiesEndpoint}/{cityId}{ClockJumpEndpoint}",
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: DownstreamServiceNames.CityCore,
                cancellationToken: cancellationToken);
        }
    }
}
