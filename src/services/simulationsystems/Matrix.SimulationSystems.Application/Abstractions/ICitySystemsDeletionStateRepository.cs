namespace Matrix.SimulationSystems.Application.Abstractions
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
