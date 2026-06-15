using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenBootstrapCreatesNothing_LogsDebugAndPassesMessageValues()
        {
            ClassicCityCreatedV1 message = CreateMessage(
                hostId: Guid.Parse("96f1def1-4d0a-4771-b7fc-f484f21f767d"),
                economyProfile: "balanced");
            var bootstrapService = new TestCityEconomyBootstrapService
            {
                Result = new CityEconomyBootstrapResultDto(
                    CityId: message.HostId,
                    BudgetCreated: false,
                    CreatedAllocations: 0,
                    CreatedBusinesses: 0,
                    UnitKind: "Currency",
                    UnitCode: "MNY",
                    UnitDisplayName: "Money",
                    UnitSymbol: "$")
            };
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                cityEconomyBootstrapService: bootstrapService,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: message.HostId,
                actual: bootstrapService.CityId);
            Assert.Equal(
                expected: message.ScenarioKey,
                actual: bootstrapService.ScenarioKey);
            Assert.Equal(
                expected: message.EconomyProfile,
                actual: bootstrapService.EconomyProfile);
            Assert.Equal(
                expected: message.CreatedAtUtc,
                actual: bootstrapService.CreatedAtUtc);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped city economy initialization",
                actualString: entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenBootstrapCreatesResources_LogsInformation()
        {
            ClassicCityCreatedV1 message = CreateMessage(
                hostId: Guid.Parse("f1bec638-b975-4e43-a8a6-a662c296c7bf"),
                economyProfile: "service-heavy");
            var bootstrapService = new TestCityEconomyBootstrapService
            {
                Result = new CityEconomyBootstrapResultDto(
                    CityId: message.HostId,
                    BudgetCreated: true,
                    CreatedAllocations: 4,
                    CreatedBusinesses: 7,
                    UnitKind: "Currency",
                    UnitCode: "MNY",
                    UnitDisplayName: "Money",
                    UnitSymbol: "$")
            };
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                cityEconomyBootstrapService: bootstrapService,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: message.HostId.ToString(),
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: message.ScenarioKey,
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: message.EconomyProfile,
                actualString: entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenCityWasDeleted_DoesNotBootstrapEconomy()
        {
            ClassicCityCreatedV1 message = CreateMessage(
                hostId: Guid.Parse("f1bec638-b975-4e43-a8a6-a662c296c7bf"),
                economyProfile: "service-heavy");
            var bootstrapService = new TestCityEconomyBootstrapService();
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                cityEconomyBootstrapService: bootstrapService,
                deletionRepository: new TestCityEconomyDeletionRepository
                {
                    DeletedAtUtc = message.CreatedAtUtc.AddMinutes(1)
                },
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            Assert.Equal(Guid.Empty, bootstrapService.CityId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, entry.LogLevel);
            Assert.Contains(message.HostId.ToString(), entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenRuntimeDoesNotMatch_DoesNotBootstrapEconomy()
        {
            ClassicCityCreatedV1 message = CreateMessage(
                hostId: Guid.NewGuid(),
                economyProfile: "balanced",
                scenarioKey: "metro",
                hostTypeKey: "network");
            var bootstrapService = new TestCityEconomyBootstrapService();
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                cityEconomyBootstrapService: bootstrapService,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            Assert.Equal(Guid.Empty, bootstrapService.CityId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Debug, entry.LogLevel);
            Assert.Contains("Ignored classic-city-created event", entry.Message);
        }

        private static ClassicCityCreatedV1 CreateMessage(
            Guid hostId,
            string economyProfile,
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey)
        {
            return new ClassicCityCreatedV1(
                SimulationId: Guid.Parse("94c5dc18-f29b-4055-8b79-fcd49ca62b76"),
                HostId: hostId,
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                Name: "Aurora",
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                DevelopmentLevel: "seed",
                EconomyProfile: economyProfile,
                RunId: Guid.Parse("df05950d-dedf-490c-93e8-c2579026bab8"),
                SimulationSeed: "economy-seed",
                ScenarioModelSetVersion: "classic-city-v3");
        }

        private sealed class TestCityEconomyBootstrapService : ICityEconomyBootstrapService
        {
            public CityEconomyBootstrapResultDto Result { get; set; } = new(
                CityId: Guid.Empty,
                BudgetCreated: false,
                CreatedAllocations: 0,
                CreatedBusinesses: 0,
                UnitKind: "Currency",
                UnitCode: "MNY",
                UnitDisplayName: "Money",
                UnitSymbol: "$");

            public Guid CityId { get; private set; }
            public string ScenarioKey { get; private set; } = string.Empty;
            public string? EconomyProfile { get; private set; }
            public DateTimeOffset CreatedAtUtc { get; private set; }

            public Task<CityEconomyBootstrapResultDto> BootstrapAsync(
                Guid cityId,
                string scenarioKey,
                string? economyProfile,
                DateTimeOffset createdAtUtc,
                CancellationToken cancellationToken = default)
            {
                CityId = cityId;
                ScenarioKey = scenarioKey;
                EconomyProfile = economyProfile;
                CreatedAtUtc = createdAtUtc;
                return Task.FromResult(Result);
            }
        }
    }
}
