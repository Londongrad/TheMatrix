using Matrix.BuildingBlocks.Domain;

namespace Matrix.BuildingBlocks.Application.Models
{
    /// <summary>
    ///     Represents a paginated result set for a query.
    /// </summary>
    /// <typeparam name="T">The type of the items contained in the result.</typeparam>
    public class PagedResult<T>(
        IReadOnlyCollection<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        /// <summary>
        ///     The collection of items returned for the current page.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; init; } = items ?? throw new ArgumentNullException(nameof(items));

        /// <summary>
        ///     The total number of items that match the query.
        /// </summary>
        public int TotalCount { get; init; } = totalCount;

        /// <summary>
        ///     The number of the current page (1-based index).
        /// </summary>
        public int PageNumber { get; init; } =
            GuardHelper.AgainstNonPositiveNumber(
                value: pageNumber,
                propertyName: nameof(PageNumber));

        /// <summary>
        ///     The number of items per page.
        /// </summary>
        public int PageSize { get; init; } =
            GuardHelper.AgainstNonPositiveNumber(
                value: pageSize,
                propertyName: nameof(PageSize));

        /// <summary>
        ///     The total number of pages based on <see cref="TotalCount" /> and <see cref="PageSize" />.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        ///     Indicates whether there is a previous page available.
        /// </summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>
        ///     Indicates whether there is a next page available.
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;
    }
}
