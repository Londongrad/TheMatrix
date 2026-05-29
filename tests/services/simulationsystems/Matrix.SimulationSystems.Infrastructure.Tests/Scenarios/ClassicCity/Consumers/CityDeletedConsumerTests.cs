using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class CityDeletedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenRuntimeDoesNotMatch_DoesNothing()
    {
        var mediator = new TestMediator();
        var logger = new TestLogger<CityDeletedConsumer>();
        var consumer = new CityDeletedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateMessage("metro", "network"), CancellationToken.None);

        Assert.Empty(mediator.Commands);
        Assert.Equal(LogLevel.Debug, Assert.Single(logger.Entries).LogLevel);
    }

    [Fact]
    public async Task ConsumeAsync_WhenRuntimeMatches_DeletesSystemsDataForHostIdentity()
    {
        SimulationDeletedV1 message = CreateMessage(
            ClassicCityRuntimeKeys.ScenarioKey,
            ClassicCityRuntimeKeys.HostTypeKey);
        var mediator = new TestMediator();
        var consumer = new CityDeletedConsumer(mediator, new TestLogger<CityDeletedConsumer>());

        await consumer.ConsumeAsync(message, CancellationToken.None);

        DeleteCitySystemsDataCommand command = Assert.Single(mediator.Commands);
        Assert.NotEqual(message.SimulationId, message.HostId);
        Assert.Equal(message.HostId, command.CityId);
        Assert.Equal(message.DeletedAtUtc, command.DeletedAtUtc);
    }

    private static SimulationDeletedV1 CreateMessage(string scenarioKey, string hostTypeKey)
    {
        return new SimulationDeletedV1(
            SimulationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            HostId: CityId,
            ScenarioKey: scenarioKey,
            HostTypeKey: hostTypeKey,
            DeletedAtUtc: LaterUtc);
    }

    private sealed class TestMediator : IMediator
    {
        public List<DeleteCitySystemsDataCommand> Commands { get; } = [];

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(Assert.IsType<DeleteCitySystemsDataCommand>(request));
            return Task.FromResult((TResponse)(object)new DeleteCitySystemsDataResult(
                DeleteCitySystemsDataStatus.Applied));
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
