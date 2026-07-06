using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;
using Matrix.Resources.Application.Tests.TestSupport;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;

public sealed class ApplyCityHealthcareMedicineDemandHandlerTests
{
    [Fact]
    public async Task Handle_NewActivity_DrainsMedicineAndPublishesSnapshotAtomically()
    {
        var repository = new FakeCityStockpileRepository { State = CreateState() };
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
        decimal originalStock = repository.State.Medicine.StockLevelIndex;
        var handler = new ApplyCityHealthcareMedicineDemandCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new CityHealthcareMedicineDemandPolicy(),
            CreateTimeProvider());

        ApplyCityHealthcareMedicineDemandResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.Equal(ApplyCityHealthcareMedicineDemandStatus.Applied, result.Status);
        Assert.Equal(0.0500m, result.MedicineLoadIndex);
        Assert.True(result.MedicineStockLevelIndex < originalStock);
        Assert.Equal(17, repository.State.HealthcareMedicineDemand.SourceRevision);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Single(outboxWriter.Snapshots);
    }

    [Fact]
    public async Task Handle_DuplicateOrStaleActivity_DoesNotConsumeAgain()
    {
        var repository = new FakeCityStockpileRepository { State = CreateState() };
        CityHealthcareMedicineDemandSnapshot current =
            new CityHealthcareMedicineDemandPolicy().CreateDemand(
                processedPatientCount: 100,
                routineCareDeliveryCount: 4,
                urgentCareDeliveryCount: 3,
                acuteCareDeliveryCount: 2,
                emergencyCareDeliveryCount: 1,
                sourceRevision: 17,
                careDate: new DateOnly(2048, 5, 6),
                observedAtUtc: CreatedAtUtc);
        repository.State.ApplyHealthcareMedicineDemand(current);
        var unitOfWork = new FakeUnitOfWork();
        var outboxWriter = new FakeCityStockpileSnapshotOutboxWriter();
        var handler = new ApplyCityHealthcareMedicineDemandCommandHandler(
            repository,
            unitOfWork,
            outboxWriter,
            new CityHealthcareMedicineDemandPolicy(),
            CreateTimeProvider());

        ApplyCityHealthcareMedicineDemandResult duplicate = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);
        ApplyCityHealthcareMedicineDemandResult stale = await handler.Handle(
            CreateCommand(sourceRevision: 16),
            CancellationToken.None);

        Assert.Equal(ApplyCityHealthcareMedicineDemandStatus.Duplicate, duplicate.Status);
        Assert.Equal(ApplyCityHealthcareMedicineDemandStatus.Stale, stale.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(outboxWriter.Snapshots);
    }

    [Fact]
    public async Task Handle_MissingStockpile_ReturnsNotInitialized()
    {
        var handler = new ApplyCityHealthcareMedicineDemandCommandHandler(
            new FakeCityStockpileRepository(),
            new FakeUnitOfWork(),
            new FakeCityStockpileSnapshotOutboxWriter(),
            new CityHealthcareMedicineDemandPolicy(),
            CreateTimeProvider());

        ApplyCityHealthcareMedicineDemandResult result = await handler.Handle(
            CreateCommand(sourceRevision: 17),
            CancellationToken.None);

        Assert.Equal(ApplyCityHealthcareMedicineDemandStatus.NotInitialized, result.Status);
    }

    private static ApplyCityHealthcareMedicineDemandCommand CreateCommand(long sourceRevision)
    {
        return new ApplyCityHealthcareMedicineDemandCommand(
            CityId: CityId,
            ProcessedPatientCount: 100,
            RoutineCareDeliveryCount: 4,
            UrgentCareDeliveryCount: 3,
            AcuteCareDeliveryCount: 2,
            EmergencyCareDeliveryCount: 1,
            SourceRevision: sourceRevision,
            CareDate: new DateOnly(2048, 5, 6),
            ObservedAtUtc: CreatedAtUtc);
    }
}
