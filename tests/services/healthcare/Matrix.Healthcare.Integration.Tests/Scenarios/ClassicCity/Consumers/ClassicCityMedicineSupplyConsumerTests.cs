using Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;
using Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityMedicineSupplyConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_MapsMedicineLineToNeutralCommand()
    {
        var mediator = new HealthcareIntegrationMediatorStub();
        var consumer = new ClassicCityMedicineSupplyConsumer(
            mediator,
            NullLogger<ClassicCityMedicineSupplyConsumer>.Instance);
        ClassicCityStockpileSnapshotV1 message = CreateMessage();

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SynchronizeCareMedicineSupplyCommand command = Assert.Single(
            mediator.MedicineSupplyCommands);
        Assert.Equal(message.CityId, command.SimulationHostId);
        Assert.Equal(message.EffectiveTickId, command.SourceRevision);
        Assert.Equal(message.Medicine.StockLevelIndex, command.StockLevelIndex);
        Assert.Equal(message.Medicine.ShortageRiskIndex, command.ShortageRiskIndex);
        Assert.Equal(message.OccurredAtUtc, command.ObservedAtUtc);
    }

    [Fact]
    public void EndpointConstants_AreStableAndBoundConcurrency()
    {
        Assert.Equal(
            "healthcare-classic-city-medicine-supply-v1",
            ClassicCityMedicineSupplyConsumerDefinition.EndpointNameValue);
        Assert.Equal(
            4,
            ClassicCityMedicineSupplyConsumerDefinition.ConcurrentMessageLimitValue);
    }

    private static ClassicCityStockpileSnapshotV1 CreateMessage()
    {
        return new ClassicCityStockpileSnapshotV1(
            CityId: Guid.NewGuid(),
            SupplyStressIndex: 0.41m,
            EmergencyRationingEnabled: false,
            Fuel: CreateLine("Fuel", 0.7m, 0.2m),
            Food: CreateLine("Food", 0.8m, 0.1m),
            Medicine: CreateLine("Medicine", 0.63m, 0.31m),
            SpareParts: CreateLine("SpareParts", 0.5m, 0.4m),
            Filters: CreateLine("Filters", 0.6m, 0.3m),
            EmergencyWater: CreateLine("EmergencyWater", 0.9m, 0.05m),
            EffectiveTickId: 17,
            EffectiveAtUtc: DateTimeOffset.Parse("2048-05-06T09:59:00+00:00"),
            OccurredAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"));
    }

    private static ClassicCityStockpileLineSnapshotV1 CreateLine(
        string kind,
        decimal stockLevel,
        decimal shortageRisk)
    {
        return new ClassicCityStockpileLineSnapshotV1(
            Kind: kind,
            StockLevelIndex: stockLevel,
            DemandPressureIndex: 0.4m,
            ResupplyReadinessIndex: 0.6m,
            ShortageRiskIndex: shortageRisk);
    }
}
