using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Resources.Application.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;

public sealed class ApplyCityHealthcareMedicineDemandCommandHandler(
    ICityStockpileRepository repository,
    IUnitOfWork unitOfWork,
    ICityStockpileSnapshotOutboxWriter outboxWriter,
    CityHealthcareMedicineDemandPolicy policy,
    TimeProvider timeProvider)
    : IRequestHandler<ApplyCityHealthcareMedicineDemandCommand, ApplyCityHealthcareMedicineDemandResult>
{
    public Task<ApplyCityHealthcareMedicineDemandResult> Handle(
        ApplyCityHealthcareMedicineDemandCommand request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            action: token => ApplyInsideTransactionAsync(request, token),
            cancellationToken: cancellationToken,
            isolationLevel: IsolationLevel.Serializable);
    }

    private async Task<ApplyCityHealthcareMedicineDemandResult> ApplyInsideTransactionAsync(
        ApplyCityHealthcareMedicineDemandCommand request,
        CancellationToken cancellationToken)
    {
        CityStockpileState? state = await repository.GetBySimulationHostIdAsync(
            new SimulationHostId(request.CityId),
            cancellationToken);
        if (state is null)
            return CreateResult(
                ApplyCityHealthcareMedicineDemandStatus.NotInitialized,
                request.SourceRevision);

        long? currentRevision = state.HealthcareMedicineDemand.SourceRevision;
        if (currentRevision == request.SourceRevision)
            return CreateResult(
                ApplyCityHealthcareMedicineDemandStatus.Duplicate,
                currentRevision.Value,
                state);
        if (currentRevision > request.SourceRevision)
            return CreateResult(
                ApplyCityHealthcareMedicineDemandStatus.Stale,
                currentRevision.Value,
                state);

        CityHealthcareMedicineDemandSnapshot demand = policy.CreateDemand(
            request.ProcessedPatientCount,
            request.RoutineCareDeliveryCount,
            request.UrgentCareDeliveryCount,
            request.AcuteCareDeliveryCount,
            request.EmergencyCareDeliveryCount,
            request.SourceRevision,
            request.CareDate,
            request.ObservedAtUtc);
        CityStockpileSnapshot refreshed = policy.ApplyConsumption(
            state.ToSnapshot(),
            demand);

        state.ApplySnapshot(refreshed);
        state.ApplyHealthcareMedicineDemand(demand);
        await outboxWriter.AddClassicCityStockpileSnapshotAsync(
            CityStockpileIntegrationEventFactory.CreateSnapshot(
                state,
                timeProvider.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResult(
            ApplyCityHealthcareMedicineDemandStatus.Applied,
            request.SourceRevision,
            state);
    }

    private static ApplyCityHealthcareMedicineDemandResult CreateResult(
        ApplyCityHealthcareMedicineDemandStatus status,
        long sourceRevision,
        CityStockpileState? state = null)
    {
        return new ApplyCityHealthcareMedicineDemandResult(
            Status: status,
            MedicineLoadIndex: state?.HealthcareMedicineDemand.MedicineLoadIndex ?? 0m,
            MedicineStockLevelIndex: state?.Medicine.StockLevelIndex ?? 0m,
            MedicineShortageRiskIndex: state?.Medicine.ShortageRiskIndex ?? 0m,
            SourceRevision: sourceRevision);
    }
}
