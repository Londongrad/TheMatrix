using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityOperationalBudgetSignalPublisher
    {
        Task PublishClassicCityOperationalBudgetPressureSnapshotAsync(
            CityOperationalBudgetPressureDto snapshot,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default);
    }
}
