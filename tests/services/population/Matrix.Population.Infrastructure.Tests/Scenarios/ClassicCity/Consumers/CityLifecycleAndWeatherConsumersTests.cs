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

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityLifecycleAndWeatherConsumersTests
    {
        [Fact]
        public async Task CityArchivedConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
        {
            var consumer = new CityArchivedConsumer(
                mediator: new LifecycleMediator(),
                logger: new TestLogger<CityArchivedConsumer>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(
                message: new CityArchivedV1(
                    CityId: Guid.Parse("b94dd9e6-ec8b-4d57-9bc5-6551e8decb22"),
                    ArchivedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
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
            var consumer = new CityDeletedConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("db4ce599-fdee-451c-89ca-fdd5df6efb98");
            CityDeletedV1 message = new(
                CityId: Guid.Parse("8a93c44e-1548-4525-b20d-e2655525f215"),
                DeletedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 10,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            DeleteCityPopulationDataCommand command = Assert.Single(mediator.DeleteCommands);
            Assert.Equal(
                expected: message.CityId,
                actual: command.CityId);
            Assert.Equal(
                expected: messageId,
                actual: command.IntegrationMessageId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Deleted population data",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityWeatherCreatedConsumer_WhenDuplicate_LogsDebug()
        {
            var mediator = new WeatherMediator
            {
                SyncResult = new SyncCityWeatherExposureStateResult(SyncCityWeatherExposureStateStatus.Duplicate)
            };
            var logger = new TestLogger<CityWeatherCreatedConsumer>();
            var consumer = new CityWeatherCreatedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateWeatherCreatedMessage(),
                messageId: Guid.Parse("d7100668-f4e5-4be5-acbc-e1cd4db8228f"),
                cancellationToken: CancellationToken.None);

            SyncCityWeatherExposureStateCommand command = Assert.Single(mediator.SyncCommands);
            Assert.Null(command.PreviousState);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "duplicate city weather exposure initialization",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityWeatherChangedConsumer_WhenApplied_SendsBothCommandsAndLogsInformation()
        {
            var mediator = new WeatherMediator
            {
                ApplyResult = new ApplyCityWeatherImpactResult(
                    Status: ApplyCityWeatherImpactStatus.Applied,
                    AffectedPeopleCount: 12),
                SyncResult = new SyncCityWeatherExposureStateResult(SyncCityWeatherExposureStateStatus.Applied)
            };
            var logger = new TestLogger<CityWeatherChangedConsumer>();
            var consumer = new CityWeatherChangedConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("78cb759e-8605-47d2-a9ee-595c91ec6985");
            CityWeatherChangedV1 message = CreateWeatherChangedMessage();

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityWeatherImpactCommand applyCommand = Assert.Single(mediator.ApplyCommands);
            SyncCityWeatherExposureStateCommand syncCommand = Assert.Single(mediator.SyncCommands);
            Assert.Equal(
                expected: messageId,
                actual: applyCommand.IntegrationMessageId);
            Assert.Equal(
                expected: CityWeatherChangedConsumerDefinition.EndpointNameValue,
                actual: applyCommand.ConsumerName);
            Assert.Equal(
                expected: $"{CityWeatherChangedConsumerDefinition.EndpointNameValue}-sync",
                actual: syncCommand.ConsumerName);
            Assert.Equal(
                expected: message.PreviousState.Type,
                actual: applyCommand.PreviousState.Type);
            Assert.Equal(
                expected: message.CurrentState.Type,
                actual: syncCommand.CurrentState.Type);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied city weather impact",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityWeatherChangedConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
        {
            var consumer = new CityWeatherChangedConsumer(
                mediator: new WeatherMediator(),
                logger: new TestLogger<CityWeatherChangedConsumer>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(
                message: CreateWeatherChangedMessage(),
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
                InitialState: CreateWeatherState(
                    type: "Clear",
                    severity: "Calm"),
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 5,
                    kind: DateTimeKind.Utc));
        }

        private static CityWeatherChangedV1 CreateWeatherChangedMessage()
        {
            return new CityWeatherChangedV1(
                CityId: Guid.Parse("2b2bd0f5-f2db-49fc-929a-b6bcf8628b11"),
                PreviousState: CreateWeatherState(
                    type: "Clear",
                    severity: "Calm"),
                CurrentState: CreateWeatherState(
                    type: "Storm",
                    severity: "Severe"),
                AtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 5,
                    kind: DateTimeKind.Utc));
        }

        private static WeatherStateV1 CreateWeatherState(
            string type,
            string severity)
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
                StartedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                ExpectedUntilUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private sealed class LifecycleMediator : IMediator
        {
            public List<ArchiveCityPopulationDataCommand> ArchiveCommands { get; } = [];
            public List<DeleteCityPopulationDataCommand> DeleteCommands { get; } = [];

            public ArchiveCityPopulationDataResult ArchiveResult { get; } =
                new(ArchiveCityPopulationDataStatus.Duplicate);

            public DeleteCityPopulationDataResult DeleteResult { get; init; } =
                new(DeleteCityPopulationDataStatus.Duplicate);

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
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

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                throw new NotSupportedException();
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
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

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                throw new NotSupportedException();
            }
        }

        private sealed class WeatherMediator : IMediator
        {
            public List<ApplyCityWeatherImpactCommand> ApplyCommands { get; } = [];
            public List<SyncCityWeatherExposureStateCommand> SyncCommands { get; } = [];

            public ApplyCityWeatherImpactResult ApplyResult { get; init; } = new(
                Status: ApplyCityWeatherImpactStatus.Duplicate,
                AffectedPeopleCount: 0);

            public SyncCityWeatherExposureStateResult SyncResult { get; init; } =
                new(SyncCityWeatherExposureStateStatus.Duplicate);

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                if (request is ApplyCityWeatherImpactCommand applyCommand)
                {
                    ApplyCommands.Add(applyCommand);
                    return Task.FromResult((TResponse)(object)ApplyResult);
                }

                SyncCityWeatherExposureStateCommand syncCommand =
                    Assert.IsType<SyncCityWeatherExposureStateCommand>(request);
                SyncCommands.Add(syncCommand);
                return Task.FromResult((TResponse)(object)SyncResult);
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                throw new NotSupportedException();
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
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

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                throw new NotSupportedException();
            }
        }
    }
}
