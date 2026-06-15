namespace Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityEconomyDeletionRepository
    {
        Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task DeleteCityDataAsync(
            Guid cityId,
            CancellationToken cancellationToken);

        Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken);
    }
}
