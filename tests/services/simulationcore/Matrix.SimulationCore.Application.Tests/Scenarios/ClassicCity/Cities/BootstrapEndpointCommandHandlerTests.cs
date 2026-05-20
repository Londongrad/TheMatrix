using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using MediatR;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;

public sealed class BootstrapEndpointCommandHandlerTests
{
    private static readonly Guid CityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OperationId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task CompletePopulationHandler_DelegatesToInternalCommand()
    {
        var mediator = new RecordingMediator
        {
            BoolResponse = true
        };
        var handler = new CompleteCityPopulationBootstrapEndpointCommandHandler(mediator);

        bool result = await handler.Handle(
            request: new CompleteCityPopulationBootstrapEndpointCommand(CityId, OperationId),
            cancellationToken: CancellationToken.None);

        Assert.True(result);
        var command = Assert.IsType<CompleteCityPopulationBootstrapCommand>(mediator.LastRequest);
        Assert.Equal(CityId, command.CityId);
        Assert.Equal(OperationId, command.OperationId);
    }

    [Fact]
    public async Task FailPopulationHandler_DelegatesToInternalCommand()
    {
        var mediator = new RecordingMediator
        {
            BoolResponse = false
        };
        var handler = new FailCityPopulationBootstrapEndpointCommandHandler(mediator);

        bool result = await handler.Handle(
            request: new FailCityPopulationBootstrapEndpointCommand(CityId, OperationId, "Population.Failed"),
            cancellationToken: CancellationToken.None);

        Assert.False(result);
        var command = Assert.IsType<FailCityPopulationBootstrapCommand>(mediator.LastRequest);
        Assert.Equal(CityId, command.CityId);
        Assert.Equal(OperationId, command.OperationId);
        Assert.Equal("Population.Failed", command.FailureCode);
    }

    [Fact]
    public async Task CompleteEconomyHandler_DelegatesToInternalCommand()
    {
        var mediator = new RecordingMediator
        {
            BoolResponse = true
        };
        var handler = new CompleteCityEconomyBootstrapEndpointCommandHandler(mediator);

        bool result = await handler.Handle(
            request: new CompleteCityEconomyBootstrapEndpointCommand(CityId, OperationId),
            cancellationToken: CancellationToken.None);

        Assert.True(result);
        var command = Assert.IsType<CompleteCityEconomyBootstrapCommand>(mediator.LastRequest);
        Assert.Equal(CityId, command.CityId);
        Assert.Equal(OperationId, command.OperationId);
    }

    [Fact]
    public async Task FailEconomyHandler_DelegatesToInternalCommand()
    {
        var mediator = new RecordingMediator
        {
            BoolResponse = true
        };
        var handler = new FailCityEconomyBootstrapEndpointCommandHandler(mediator);

        bool result = await handler.Handle(
            request: new FailCityEconomyBootstrapEndpointCommand(CityId, OperationId, "Economy.Failed"),
            cancellationToken: CancellationToken.None);

        Assert.True(result);
        var command = Assert.IsType<FailCityEconomyBootstrapCommand>(mediator.LastRequest);
        Assert.Equal(CityId, command.CityId);
        Assert.Equal(OperationId, command.OperationId);
        Assert.Equal("Economy.Failed", command.FailureCode);
    }

    private sealed class RecordingMediator : IMediator
    {
        public bool BoolResponse { get; init; }
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)(object)BoolResponse);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(BoolResponse);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish(
            object notification,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }
}
