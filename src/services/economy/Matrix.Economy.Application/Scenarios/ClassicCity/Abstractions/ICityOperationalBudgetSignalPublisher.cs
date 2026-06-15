using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
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
