using Matrix.BuildingBlocks.Domain;

namespace Matrix.BuildingBlocks.Application.Models
{
    public sealed class CursorPagedResult<T>(
        IReadOnlyCollection<T> items,
        int pageSize,
        string? nextCursor)
    {
        public IReadOnlyCollection<T> Items { get; init; } = items ?? throw new ArgumentNullException(nameof(items));

        public int PageSize { get; init; } =
            GuardHelper.AgainstNonPositiveNumber(
                value: pageSize,
                propertyName: nameof(PageSize));

        public string? NextCursor { get; init; } = string.IsNullOrWhiteSpace(nextCursor)
            ? null
            : nextCursor;

        public bool HasNext => NextCursor is not null;
    }
}
