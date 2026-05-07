using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Consumers;

public sealed class CityCreatedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenBootstrapCreatesNothing_LogsDebugAndPassesMessageValues()
    {
        CityCreatedV1 message = new(
            CityId: Guid.Parse("96f1def1-4d0a-4771-b7fc-f484f21f767d"),
            Name: "Novy Mir",
            SimulationKind: "classic-city",
            CreatedAtUtc: new DateTimeOffset(2048, 5, 6, 8, 15, 0, TimeSpan.Zero),
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
        var consumer = new CityCreatedConsumer(bootstrapService, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        Assert.Equal(message.CityId, bootstrapService.CityId);
        Assert.Equal(message.SimulationKind, bootstrapService.SimulationKind);
        Assert.Equal(message.EconomyProfile, bootstrapService.EconomyProfile);
        Assert.Equal(message.CreatedAtUtc, bootstrapService.CreatedAtUtc);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped city economy initialization", entry.Message);
    }

    [Fact]
    public async Task ConsumeAsync_WhenBootstrapCreatesResources_LogsInformation()
    {
        CityCreatedV1 message = new(
            CityId: Guid.Parse("f1bec638-b975-4e43-a8a6-a662c296c7bf"),
            Name: "Aurora",
            SimulationKind: "classic-city",
            CreatedAtUtc: new DateTimeOffset(2048, 5, 6, 8, 30, 0, TimeSpan.Zero),
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
        var consumer = new CityCreatedConsumer(bootstrapService, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains(message.CityId.ToString(), entry.Message);
        Assert.Contains(message.SimulationKind, entry.Message);
        Assert.Contains(message.EconomyProfile ?? string.Empty, entry.Message);
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
