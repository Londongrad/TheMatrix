using MediatR;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceRunningSimulations;
using Matrix.SimulationCore.Infrastructure.HostedServices;
using Matrix.SimulationCore.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.HostedServices;

public sealed class SimulationTickHostedServiceTests
{
    [Fact]
    public async Task StartAsync_WhenPeriodMillisecondsIsNonPositive_ThrowsInvalidOperationException()
    {
        var service = CreateService(
            periodMilliseconds: 0,
            mediator: new TestMediator(),
            logger: new TestLogger<SimulationTickHostedService>());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        Assert.Contains("PeriodMilliseconds must be > 0", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenTickOccurs_SendsAdvanceRunningSimulationsCommand()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var mediator = new TestMediator
        {
            OnSend = _ => cancellationTokenSource.Cancel()
        };
        var service = CreateService(
            periodMilliseconds: 20,
            mediator: mediator,
            logger: new TestLogger<SimulationTickHostedService>());

        await service.StartAsync(cancellationTokenSource.Token);

        AdvanceRunningSimulationsCommand command = await mediator.CommandReceived.Task.WaitAsync(
            TimeSpan.FromSeconds(1));
        await service.StopAsync(CancellationToken.None);

        Assert.Single(mediator.Commands);
        Assert.True(command.RealDelta > TimeSpan.Zero);
    }

    [Fact]
    public async Task StartAsync_WhenMediatorThrows_LogsErrorAndStopsOnCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var mediator = new TestMediator
        {
            OnSend = _ => cancellationTokenSource.Cancel(),
            ExceptionToThrow = new InvalidOperationException("boom")
        };
        var logger = new TestLogger<SimulationTickHostedService>();
        var service = CreateService(
            periodMilliseconds: 20,
            mediator: mediator,
            logger: logger);

        await service.StartAsync(cancellationTokenSource.Token);

        await mediator.CommandReceived.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await service.StopAsync(CancellationToken.None);

        Assert.Single(mediator.Commands);
        TestLogEntry errorEntry = Assert.Single(logger.Entries, x => x.LogLevel == LogLevel.Error);
        Assert.Contains("SimulationCore tick loop iteration failed.", errorEntry.Message);
        Assert.IsType<InvalidOperationException>(errorEntry.Exception);
    }

    private static SimulationTickHostedService CreateService(
        int periodMilliseconds,
        IMediator mediator,
        ILogger<SimulationTickHostedService> logger)
    {
        var serviceProvider = new HostedServicesTestSupport.DictionaryServiceProvider(
            new Dictionary<Type, object>
            {
                [typeof(IMediator)] = mediator
            });

        return new SimulationTickHostedService(
            scopeFactory: new HostedServicesTestSupport.TestServiceScopeFactory(serviceProvider),
            options: Microsoft.Extensions.Options.Options.Create(
                new SimulationTickOptions { PeriodMilliseconds = periodMilliseconds }),
            logger: logger);
    }

    private sealed class TestMediator : IMediator
    {
        public List<AdvanceRunningSimulationsCommand> Commands { get; } = [];
        public TaskCompletionSource<AdvanceRunningSimulationsCommand> CommandReceived { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Action<AdvanceRunningSimulationsCommand>? OnSend { get; set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            AdvanceRunningSimulationsCommand command = Assert.IsType<AdvanceRunningSimulationsCommand>(request);
            Commands.Add(command);
            OnSend?.Invoke(command);
            CommandReceived.TrySetResult(command);

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            object response = new AdvanceRunningSimulationsResult(
                ProcessedCount: 1,
                AdvancedCount: 1,
                SkippedCount: 0,
                FailedCount: 0);

            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<TestLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new TestLogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
