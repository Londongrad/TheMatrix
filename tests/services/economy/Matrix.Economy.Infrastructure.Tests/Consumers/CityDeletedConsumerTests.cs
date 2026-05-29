using Matrix.Economy.Application.UseCases.Lifecycle.DeleteCityEconomyData;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Consumers;

public sealed class CityDeletedConsumerTests
{
    [Fact]
    public async Task Consume_WhenRuntimeDoesNotMatch_DoesNothing()
    {
        var mediator = new TestMediator();
        var logger = new TestLogger<CityDeletedConsumer>();
        var consumer = new CityDeletedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateMessage("metro", "network"), CancellationToken.None);

        Assert.Empty(mediator.Commands);
        Assert.Equal(LogLevel.Debug, Assert.Single(logger.Entries).LogLevel);
    }

    [Fact]
    public async Task Consume_WhenRuntimeMatches_DeletesEconomyForHostIdentity()
    {
        SimulationDeletedV1 message = CreateMessage(
            ClassicCityRuntimeKeys.ScenarioKey,
            ClassicCityRuntimeKeys.HostTypeKey);
        var mediator = new TestMediator();
        var consumer = new CityDeletedConsumer(mediator, new TestLogger<CityDeletedConsumer>());

        await consumer.ConsumeAsync(message, CancellationToken.None);

        DeleteCityEconomyDataCommand command = Assert.Single(mediator.Commands);
        Assert.NotEqual(message.SimulationId, message.HostId);
        Assert.Equal(message.HostId, command.CityId);
        Assert.Equal(message.DeletedAtUtc, command.DeletedAtUtc);
    }

    private static SimulationDeletedV1 CreateMessage(string scenarioKey, string hostTypeKey)
    {
        return new SimulationDeletedV1(
            SimulationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            HostId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ScenarioKey: scenarioKey,
            HostTypeKey: hostTypeKey,
            DeletedAtUtc: DateTimeOffset.Parse("2048-05-07T08:00:00+00:00"));
    }

    private sealed class TestMediator : IMediator
    {
        public List<DeleteCityEconomyDataCommand> Commands { get; } = [];

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(Assert.IsType<DeleteCityEconomyDataCommand>(request));
            return Task.FromResult((TResponse)(object)new DeleteCityEconomyDataResult(
                DeleteCityEconomyDataStatus.Applied));
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
