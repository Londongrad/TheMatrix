namespace Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityResourceDeletionStateRepository
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
