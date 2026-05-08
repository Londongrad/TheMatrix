using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityWeatherImpact;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class CityLifecycleAndWeatherConsumersTests
{
    [Fact]
    public async Task CityArchivedConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
    {
        var consumer = new CityArchivedConsumer(
            mediator: new LifecycleMediator(),
            logger: new TestLogger<CityArchivedConsumer>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.ConsumeAsync(
                new CityArchivedV1(
                    CityId: Guid.Parse("b94dd9e6-ec8b-4d57-9bc5-6551e8decb22"),
                    ArchivedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero)),
                messageId: null,
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task CityDeletedConsumer_WhenApplied_SendsMappedCommandAndLogsInformation()
    {
        var mediator = new LifecycleMediator
        {
            DeleteResult = new DeleteCityPopulationDataResult(DeleteCityPopulationDataStatus.Applied)
        };
        var logger = new TestLogger<CityDeletedConsumer>();
        var consumer = new CityDeletedConsumer(mediator, logger);
        Guid messageId = Guid.Parse("db4ce599-fdee-451c-89ca-fdd5df6efb98");
        CityDeletedV1 message = new(
            CityId: Guid.Parse("8a93c44e-1548-4525-b20d-e2655525f215"),
            DeletedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 30, 0, TimeSpan.Zero));

        await consumer.ConsumeAsync(message, messageId, CancellationToken.None);

        DeleteCityPopulationDataCommand command = Assert.Single(mediator.DeleteCommands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(messageId, command.IntegrationMessageId);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Deleted population data", entry.Message);
    }

    [Fact]
    public async Task CityWeatherCreatedConsumer_WhenDuplicate_LogsDebug()
    {
        var mediator = new WeatherMediator
        {
            SyncResult = new SyncCityWeatherExposureStateResult(SyncCityWeatherExposureStateStatus.Duplicate)
        };
        var logger = new TestLogger<CityWeatherCreatedConsumer>();
        var consumer = new CityWeatherCreatedConsumer(mediator, logger);

        await consumer.ConsumeAsync(
            CreateWeatherCreatedMessage(),
            Guid.Parse("d7100668-f4e5-4be5-acbc-e1cd4db8228f"),
            CancellationToken.None);

        SyncCityWeatherExposureStateCommand command = Assert.Single(mediator.SyncCommands);
        Assert.Null(command.PreviousState);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("duplicate city weather exposure initialization", entry.Message);
    }

    [Fact]
    public async Task CityWeatherChangedConsumer_WhenApplied_SendsBothCommandsAndLogsInformation()
    {
        var mediator = new WeatherMediator
        {
            ApplyResult = new ApplyCityWeatherImpactResult(ApplyCityWeatherImpactStatus.Applied, 12),
            SyncResult = new SyncCityWeatherExposureStateResult(SyncCityWeatherExposureStateStatus.Applied)
        };
        var logger = new TestLogger<CityWeatherChangedConsumer>();
        var consumer = new CityWeatherChangedConsumer(mediator, logger);
        Guid messageId = Guid.Parse("78cb759e-8605-47d2-a9ee-595c91ec6985");
        CityWeatherChangedV1 message = CreateWeatherChangedMessage();

        await consumer.ConsumeAsync(message, messageId, CancellationToken.None);

        ApplyCityWeatherImpactCommand applyCommand = Assert.Single(mediator.ApplyCommands);
        SyncCityWeatherExposureStateCommand syncCommand = Assert.Single(mediator.SyncCommands);
        Assert.Equal(messageId, applyCommand.IntegrationMessageId);
        Assert.Equal(CityWeatherChangedConsumerDefinition.EndpointNameValue, applyCommand.ConsumerName);
        Assert.Equal($"{CityWeatherChangedConsumerDefinition.EndpointNameValue}-sync", syncCommand.ConsumerName);
        Assert.Equal(message.PreviousState.Type, applyCommand.PreviousState.Type);
        Assert.Equal(message.CurrentState.Type, syncCommand.CurrentState.Type);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied city weather impact", entry.Message);
    }

    [Fact]
    public async Task CityWeatherChangedConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
    {
        var consumer = new CityWeatherChangedConsumer(
            mediator: new WeatherMediator(),
            logger: new TestLogger<CityWeatherChangedConsumer>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => consumer.ConsumeAsync(
                CreateWeatherChangedMessage(),
                messageId: null,
                cancellationToken: CancellationToken.None));
    }

    private static CityWeatherCreatedV1 CreateWeatherCreatedMessage()
    {
        return new CityWeatherCreatedV1(
            CityId: Guid.Parse("2b2bd0f5-f2db-49fc-929a-b6bcf8628b11"),
            ClimateProfile: new WeatherClimateProfileV1(
                ClimateZone: "Temperate",
                Volatility: 0.3m,
                MaxOverallSeverity: "Severe",
                SupportsThunderstorms: true,
                SupportsSnowstorms: false,
                SupportsFog: true,
                SupportsHeatwaves: true),
            InitialState: CreateWeatherState("Clear", "Calm"),
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 6, 11, 0, 5, DateTimeKind.Utc));
    }

    private static CityWeatherChangedV1 CreateWeatherChangedMessage()
    {
        return new CityWeatherChangedV1(
            CityId: Guid.Parse("2b2bd0f5-f2db-49fc-929a-b6bcf8628b11"),
            PreviousState: CreateWeatherState("Clear", "Calm"),
            CurrentState: CreateWeatherState("Storm", "Severe"),
            AtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero),
            OccurredOnUtc: new DateTime(2048, 5, 6, 12, 0, 5, DateTimeKind.Utc));
    }

    private static WeatherStateV1 CreateWeatherState(string type, string severity)
    {
        return new WeatherStateV1(
            Type: type,
            Severity: severity,
            PrecipitationKind: "None",
            TemperatureC: 18m,
            HumidityPercent: 50m,
            WindSpeedKph: 10m,
            CloudCoveragePercent: 20m,
            PressureHpa: 1012m,
            StartedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            ExpectedUntilUtc: new DateTimeOffset(2048, 5, 6, 13, 0, 0, TimeSpan.Zero));
    }

    private sealed class LifecycleMediator : IMediator
    {
        public List<ArchiveCityPopulationDataCommand> ArchiveCommands { get; } = [];
        public List<DeleteCityPopulationDataCommand> DeleteCommands { get; } = [];
        public ArchiveCityPopulationDataResult ArchiveResult { get; init; } = new(ArchiveCityPopulationDataStatus.Duplicate);
        public DeleteCityPopulationDataResult DeleteResult { get; init; } = new(DeleteCityPopulationDataStatus.Duplicate);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ArchiveCityPopulationDataCommand archiveCommand)
            {
                ArchiveCommands.Add(archiveCommand);
                return Task.FromResult((TResponse)(object)ArchiveResult);
            }

            DeleteCityPopulationDataCommand deleteCommand = Assert.IsType<DeleteCityPopulationDataCommand>(request);
            DeleteCommands.Add(deleteCommand);
            return Task.FromResult((TResponse)(object)DeleteResult);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();
    }

    private sealed class WeatherMediator : IMediator
    {
        public List<ApplyCityWeatherImpactCommand> ApplyCommands { get; } = [];
        public List<SyncCityWeatherExposureStateCommand> SyncCommands { get; } = [];
        public ApplyCityWeatherImpactResult ApplyResult { get; init; } = new(ApplyCityWeatherImpactStatus.Duplicate, 0);
        public SyncCityWeatherExposureStateResult SyncResult { get; init; } = new(SyncCityWeatherExposureStateStatus.Duplicate);

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ApplyCityWeatherImpactCommand applyCommand)
            {
                ApplyCommands.Add(applyCommand);
                return Task.FromResult((TResponse)(object)ApplyResult);
            }

            SyncCityWeatherExposureStateCommand syncCommand = Assert.IsType<SyncCityWeatherExposureStateCommand>(request);
            SyncCommands.Add(syncCommand);
            return Task.FromResult((TResponse)(object)SyncResult);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();
    }
}
