using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Consumers
{
    public sealed class CityCreatedConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenBootstrapCreatesNothing_LogsDebugAndPassesMessageValues()
        {
            CityCreatedV1 message = new(
                CityId: Guid.Parse("96f1def1-4d0a-4771-b7fc-f484f21f767d"),
                Name: "Novy Mir",
                SimulationKind: "classic-city",
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero),
                DevelopmentLevel: "seed",
                EconomyProfile: "balanced");
            var bootstrapService = new TestCityEconomyBootstrapService
            {
                Result = new CityEconomyBootstrapResultDto(
                    CityId: message.CityId,
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
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: message.CityId,
                actual: bootstrapService.CityId);
            Assert.Equal(
                expected: message.SimulationKind,
                actual: bootstrapService.SimulationKind);
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
            CityCreatedV1 message = new(
                CityId: Guid.Parse("f1bec638-b975-4e43-a8a6-a662c296c7bf"),
                Name: "Aurora",
                SimulationKind: "classic-city",
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                DevelopmentLevel: "seed",
                EconomyProfile: "service-heavy");
            var bootstrapService = new TestCityEconomyBootstrapService
            {
                Result = new CityEconomyBootstrapResultDto(
                    CityId: message.CityId,
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
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: message.CityId.ToString(),
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: message.SimulationKind,
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: message.EconomyProfile ?? string.Empty,
                actualString: entry.Message);
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
            public string SimulationKind { get; private set; } = string.Empty;
            public string? EconomyProfile { get; private set; }
            public DateTimeOffset CreatedAtUtc { get; private set; }

            public Task<CityEconomyBootstrapResultDto> BootstrapAsync(
                Guid cityId,
                string simulationKind,
                string? economyProfile,
                DateTimeOffset createdAtUtc,
                CancellationToken cancellationToken = default)
            {
                CityId = cityId;
                SimulationKind = simulationKind;
                EconomyProfile = economyProfile;
                CreatedAtUtc = createdAtUtc;
                return Task.FromResult(Result);
            }
        }
    }
}
