using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class SimulationDeletedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenRuntimeDoesNotMatch_DoesNotDeleteCityResources()
    {
        var mediator = new TestMediator();
        var logger = new TestLogger<SimulationDeletedConsumer>();
        var consumer = new SimulationDeletedConsumer(mediator, logger);

        await consumer.ConsumeAsync(
            CreateMessage(scenarioKey: "metro", hostTypeKey: "network"),
            CancellationToken.None);

        Assert.Empty(mediator.Commands);
        Assert.Equal(LogLevel.Debug, Assert.Single(logger.Entries).LogLevel);
    }

    [Fact]
    public async Task ConsumeAsync_WhenRuntimeMatches_DeletesResourcesForHostIdentity()
    {
        SimulationDeletedV1 message = CreateMessage(
            scenarioKey: ClassicCityRuntimeKeys.ScenarioKey,
            hostTypeKey: ClassicCityRuntimeKeys.HostTypeKey);
        var mediator = new TestMediator();
        var consumer = new SimulationDeletedConsumer(
            mediator,
            new TestLogger<SimulationDeletedConsumer>());

        await consumer.ConsumeAsync(message, CancellationToken.None);

        DeleteCityResourcesCommand command = Assert.Single(mediator.Commands);
        Assert.NotEqual(message.SimulationId, message.HostId);
        Assert.Equal(message.HostId, command.CityId);
        Assert.Equal(message.DeletedAtUtc, command.DeletedAtUtc);
    }

    private static SimulationDeletedV1 CreateMessage(string scenarioKey, string hostTypeKey)
    {
        return new SimulationDeletedV1(
            SimulationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            HostId: ResourcesInfrastructureTestSupport.CityId,
            ScenarioKey: scenarioKey,
            HostTypeKey: hostTypeKey,
            DeletedAtUtc: ResourcesInfrastructureTestSupport.LaterUtc);
    }

    private sealed class TestMediator : IMediator
    {
        public List<DeleteCityResourcesCommand> Commands { get; } = [];

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(Assert.IsType<DeleteCityResourcesCommand>(request));
            return Task.FromResult((TResponse)(object)new DeleteCityResourcesResult(
                DeleteCityResourcesStatus.Applied));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification => throw new NotSupportedException();
    }
}
