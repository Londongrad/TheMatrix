using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using Matrix.Resources.Application.Tests.TestSupport;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing;

public sealed class SetCityEmergencyRationingTests
{
    [Fact]
    public void Validator_RejectsEmptyCityId()
    {
        var validator = new SetCityEmergencyRationingCommandValidator();

        var result = validator.Validate(new SetCityEmergencyRationingCommand(Guid.Empty, true));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Handler_ReturnsNotInitializedWhenStateIsMissing()
    {
        var handler = new SetCityEmergencyRationingCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        SetCityEmergencyRationingResult result = await handler.Handle(
            new SetCityEmergencyRationingCommand(CityId, true),
            CancellationToken.None);

        Assert.Equal(SetCityEmergencyRationingStatus.NotInitialized, result.Status);
    }

    [Fact]
    public async Task Handler_ReturnsDuplicateWhenRequestedFlagMatchesCurrentState()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState(emergencyRationingEnabled: true)
        };
        var handler = new SetCityEmergencyRationingCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityStockpilePolicy(),
            CreateTimeProvider());

        SetCityEmergencyRationingResult result = await handler.Handle(
            new SetCityEmergencyRationingCommand(CityId, true),
            CancellationToken.None);

        Assert.Equal(SetCityEmergencyRationingStatus.Duplicate, result.Status);
        Assert.True(result.EmergencyRationingEnabled);
    }

    [Fact]
    public async Task Handler_AppliesRationingAndWritesSnapshotWithInjectedTime()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState(emergencyRationingEnabled: false)
        };
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
        DateTimeOffset occurredAtUtc = LaterUtc.AddMinutes(30);
        var handler = new SetCityEmergencyRationingCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new CityStockpilePolicy(),
            CreateTimeProvider(occurredAtUtc));

        SetCityEmergencyRationingResult result = await handler.Handle(
            new SetCityEmergencyRationingCommand(CityId, true),
            CancellationToken.None);

        Assert.Equal(SetCityEmergencyRationingStatus.Applied, result.Status);
        Assert.True(repository.State!.EmergencyRationingEnabled);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(outboxWriter.Snapshots);
        Assert.Equal(occurredAtUtc, outboxWriter.Snapshots[0].OccurredAtUtc);
    }
}
