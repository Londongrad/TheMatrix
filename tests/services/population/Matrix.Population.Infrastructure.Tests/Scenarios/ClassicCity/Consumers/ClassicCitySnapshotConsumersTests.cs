using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCitySnapshotConsumersTests
    {
        [Fact]
        public async Task CostOfLivingConsumer_WhenApplied_SendsMappedCommandAndLogsInformation()
        {
            var mediator = new SnapshotMediator
            {
                CostOfLivingResult =
                    new ApplyCityCostOfLivingSnapshotResult(ApplyCityCostOfLivingSnapshotStatus.Applied)
            };
            var logger = new TestLogger<ClassicCityCostOfLivingSnapshotConsumer>();
            var consumer = new ClassicCityCostOfLivingSnapshotConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("46642b2d-4360-446a-bdf0-99ca52c6b2c0");
            ClassicCityCostOfLivingSnapshotV1 message = new(
                CityId: Guid.Parse("d757c990-0a5c-4f37-a483-d2bde4d2cf7e"),
                WageMultiplier: 1.1m,
                RetailPriceMultiplier: 1.2m,
                HousingCostMultiplier: 1.3m,
                UtilityCostMultiplier: 1.4m,
                CostOfLivingIndex: 0.9m,
                AffordabilityIndex: 0.8m,
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityCostOfLivingSnapshotCommand command = Assert.Single(mediator.CostOfLivingCommands);
            Assert.Equal(
                expected: messageId,
                actual: command.IntegrationMessageId);
            Assert.Equal(
                expected: 1.4m,
                actual: command.UtilityCostMultiplier);
            Assert.Equal(
                expected: 0.8m,
                actual: command.AffordabilityIndex);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied classic city cost-of-living snapshot",
                actualString: entry.Message);
        }

        [Fact]
        public async Task ServiceQualityConsumer_WhenStale_LogsDebug()
        {
            var mediator = new SnapshotMediator
            {
                ServiceQualityResult =
                    new ApplyCityServiceQualitySnapshotResult(ApplyCityServiceQualitySnapshotStatus.Stale)
            };
            var logger = new TestLogger<ClassicCityServiceQualitySnapshotConsumer>();
            var consumer = new ClassicCityServiceQualitySnapshotConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: new ClassicCityServiceQualitySnapshotV1(
                    CityId: Guid.Parse("72ab8401-11c2-47af-bd65-ce27d4217f4f"),
                    HealthcareQualityIndex: 0.7m,
                    EducationQualityIndex: 0.8m,
                    HousingSupportIndex: 0.9m,
                    OccurredAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 14,
                        minute: 15,
                        second: 0,
                        offset: TimeSpan.Zero)),
                messageId: Guid.Parse("f25f7ca8-f4eb-49a6-91ff-f91d60b51403"),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "stale classic city service-quality snapshot",
                actualString: entry.Message);
        }

        [Fact]
        public async Task LivingConditionsConsumer_WhenApplied_SendsMappedCommand()
        {
            var mediator = new SnapshotMediator
            {
                LivingConditionsResult =
                    new ApplyCityLivingConditionsSnapshotResult(ApplyCityLivingConditionsSnapshotStatus.Applied)
            };
            var logger = new TestLogger<ClassicCityLivingConditionsSnapshotConsumer>();
            var consumer = new ClassicCityLivingConditionsSnapshotConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("45f8f439-40fe-4714-a75c-35f71318a5cb");
            ClassicCityLivingConditionsSnapshotV1 message = new(
                CityId: Guid.Parse("bc0fbc3f-b6ce-44ca-a9ce-5622dabfb4a9"),
                FloodingIndex: 0.1m,
                RoadAccessibilityIndex: 0.2m,
                PowerCoverageIndex: 0.3m,
                UtilityContinuityIndex: 0.4m,
                HeatingCoverageIndex: 0.5m,
                WaterCoverageIndex: 0.6m,
                SanitationCoverageIndex: 0.7m,
                EffectiveTickId: 13,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 14,
                    minute: 31,
                    second: 0,
                    offset: TimeSpan.Zero));

            await consumer.ConsumeAsync(
                message: message,
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityLivingConditionsSnapshotCommand command = Assert.Single(mediator.LivingConditionsCommands);
            Assert.Equal(
                expected: 13,
                actual: command.EffectiveTickId);
            Assert.Equal(
                expected: 0.7m,
                actual: command.SanitationCoverageIndex);
        }

        [Fact]
        public async Task StockpileConsumer_WhenMessageIdIsMissing_ThrowsInvalidOperationException()
        {
            var consumer = new ClassicCityStockpileSnapshotConsumer(
                mediator: new SnapshotMediator(),
                logger: new TestLogger<ClassicCityStockpileSnapshotConsumer>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.ConsumeAsync(
                message: CreateStockpileSnapshot(),
                messageId: null,
                cancellationToken: CancellationToken.None));
        }

        [Fact]
        public async Task StockpileConsumer_WhenCityArchived_LogsDebugAndMapsEssentials()
        {
            var mediator = new SnapshotMediator
            {
                EssentialsResult = new ApplyCityEssentialsSnapshotResult(ApplyCityEssentialsSnapshotStatus.CityArchived)
            };
            var logger = new TestLogger<ClassicCityStockpileSnapshotConsumer>();
            var consumer = new ClassicCityStockpileSnapshotConsumer(
                mediator: mediator,
                logger: logger);
            var messageId = Guid.Parse("40f8f5c7-b7e0-45ec-a0e3-c07e29ea90a1");

            await consumer.ConsumeAsync(
                message: CreateStockpileSnapshot(),
                messageId: messageId,
                cancellationToken: CancellationToken.None);

            ApplyCityEssentialsSnapshotCommand command = Assert.Single(mediator.EssentialsCommands);
            Assert.Equal(
                expected: messageId,
                actual: command.IntegrationMessageId);
            Assert.Equal(
                expected: 0.4m,
                actual: command.FoodStockLevelIndex);
            Assert.Equal(
                expected: 0.9m,
                actual: command.EmergencyWaterShortageRiskIndex);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "archived",
                actualString: entry.Message);
        }

        private static ClassicCityStockpileSnapshotV1 CreateStockpileSnapshot()
        {
            ClassicCityStockpileLineSnapshotV1 line(
                string kind,
                decimal stock,
                decimal shortage)
            {
                return new ClassicCityStockpileLineSnapshotV1(
                    Kind: kind,
                    StockLevelIndex: stock,
                    DemandPressureIndex: 0.2m,
                    ResupplyReadinessIndex: 0.3m,
                    ShortageRiskIndex: shortage);
            }

            return new ClassicCityStockpileSnapshotV1(
                CityId: Guid.Parse("3ffad8c2-9cc9-4cf5-834e-9cdd61d6ca10"),
                SupplyStressIndex: 0.6m,
                EmergencyRationingEnabled: true,
                Fuel: line(
                    kind: "Fuel",
                    stock: 0.1m,
                    shortage: 0.7m),
                Food: line(
                    kind: "Food",
                    stock: 0.4m,
                    shortage: 0.5m),
                Medicine: line(
                    kind: "Medicine",
                    stock: 0.6m,
                    shortage: 0.8m),
                SpareParts: line(
                    kind: "SpareParts",
                    stock: 0.3m,
                    shortage: 0.4m),
                Filters: line(
                    kind: "Filters",
                    stock: 0.2m,
                    shortage: 0.6m),
                EmergencyWater: line(
                    kind: "EmergencyWater",
                    stock: 0.7m,
                    shortage: 0.9m),
                EffectiveTickId: 21,
                EffectiveAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 15,
                    minute: 1,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private sealed class SnapshotMediator : IMediator
        {
            public List<ApplyCityCostOfLivingSnapshotCommand> CostOfLivingCommands { get; } = [];
            public List<ApplyCityServiceQualitySnapshotCommand> ServiceQualityCommands { get; } = [];
            public List<ApplyCityLivingConditionsSnapshotCommand> LivingConditionsCommands { get; } = [];
            public List<ApplyCityEssentialsSnapshotCommand> EssentialsCommands { get; } = [];

            public ApplyCityCostOfLivingSnapshotResult CostOfLivingResult { get; init; } =
                new(ApplyCityCostOfLivingSnapshotStatus.Duplicate);

            public ApplyCityServiceQualitySnapshotResult ServiceQualityResult { get; init; } =
                new(ApplyCityServiceQualitySnapshotStatus.Duplicate);

            public ApplyCityLivingConditionsSnapshotResult LivingConditionsResult { get; init; } =
                new(ApplyCityLivingConditionsSnapshotStatus.Duplicate);

            public ApplyCityEssentialsSnapshotResult EssentialsResult { get; init; } =
                new(ApplyCityEssentialsSnapshotStatus.Duplicate);

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                switch (request)
                {
                    case ApplyCityCostOfLivingSnapshotCommand costOfLivingCommand:
                        CostOfLivingCommands.Add(costOfLivingCommand);
                        return Task.FromResult((TResponse)(object)CostOfLivingResult);
                    case ApplyCityServiceQualitySnapshotCommand serviceQualityCommand:
                        ServiceQualityCommands.Add(serviceQualityCommand);
                        return Task.FromResult((TResponse)(object)ServiceQualityResult);
                    case ApplyCityLivingConditionsSnapshotCommand livingConditionsCommand:
                        LivingConditionsCommands.Add(livingConditionsCommand);
                        return Task.FromResult((TResponse)(object)LivingConditionsResult);
                    case ApplyCityEssentialsSnapshotCommand essentialsCommand:
                        EssentialsCommands.Add(essentialsCommand);
                        return Task.FromResult((TResponse)(object)EssentialsResult);
                    default:
                        throw new NotSupportedException();
                }
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
