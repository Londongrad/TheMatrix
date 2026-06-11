namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICitySystemsDeletionStateRepository
    {
        Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken);
    }
}
