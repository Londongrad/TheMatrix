using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using MediatR;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Provisioning;

internal static class ProvisioningTestSupport
{
    internal sealed class FakeMediator : IMediator
    {
        public Func<object, object?>? SendHandler { get; init; }
        public List<object> SentRequests { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult((TResponse)(SendHandler?.Invoke(request) ?? default(TResponse)!));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            SentRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult(SendHandler?.Invoke(request));
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    internal sealed class FakeCityEconomyBootstrapClient : ICityEconomyBootstrapClient
    {
        public Guid? RequestedCityId { get; private set; }
        public string? RequestedSimulationKind { get; private set; }
        public string? RequestedEconomyProfile { get; private set; }
        public DateTimeOffset? RequestedCreatedAtUtc { get; private set; }
        public CityEconomyBootstrapResult? Result { get; init; }
        public Exception? ExceptionToThrow { get; init; }

        public Task<CityEconomyBootstrapResult> InitializeAsync(
            Guid cityId,
            string simulationKind,
            string economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            RequestedSimulationKind = simulationKind;
            RequestedEconomyProfile = economyProfile;
            RequestedCreatedAtUtc = createdAtUtc;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return Task.FromResult(Result ?? new CityEconomyBootstrapResult(null, null, null, null));
        }
    }

    internal sealed class FakeCityPopulationBootstrapClient : ICityPopulationBootstrapClient
    {
        public CityPopulationBootstrapInitializationRequest? RequestedRequest { get; private set; }
        public CityPopulationBootstrapSummary? Result { get; init; }
        public Exception? ExceptionToThrow { get; init; }

        public Task<CityPopulationBootstrapSummary> InitializeAsync(
            CityPopulationBootstrapInitializationRequest request,
            CancellationToken cancellationToken)
        {
            RequestedRequest = request;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            return Task.FromResult(Result ?? throw new NotSupportedException());
        }
    }
}
