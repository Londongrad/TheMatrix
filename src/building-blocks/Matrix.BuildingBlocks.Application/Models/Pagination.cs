namespace Matrix.BuildingBlocks.Application.Models
{
    /// <summary>
    ///     Represents pagination parameters for data retrieval operations.
    /// </summary>
    /// <remarks>
    ///     This record encapsulates paging logic and ensures that both the page number and page size
    ///     are always within valid bounds. It is typically used when retrieving subsets of large datasets,
    ///     providing convenient access to the number of items to skip and take for a paged query.
    /// </remarks>
    public sealed record Pagination
    {
        /// <summary>
        ///     The maximum number of items that can be included in a single page.
        /// </summary>
        public const int MaxPageSize = 500;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Pagination" /> record with the specified parameters.
        /// </summary>
        /// <param name="pageNumber">The number of the page to retrieve. Must be greater than 0.</param>
        /// <param name="pageSize">
        ///     The number of items to include in a single page. Must be between 1 and
        ///     <see cref="MaxPageSize" />.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="pageNumber" /> is less than 1,
        ///     or when <paramref name="pageSize" /> is less than 1 or greater than <see cref="MaxPageSize" />.
        /// </exception>
        /// <remarks>
        ///     This constructor enforces strict validation of pagination parameters to ensure predictable behavior.
        ///     If invalid arguments are supplied, the constructor throws an <see cref="ArgumentOutOfRangeException" />
        ///     rather than silently correcting them. This approach helps detect programming errors early
        ///     and maintain consistency across data retrieval operations.
        /// </remarks>
        public Pagination(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(paramName: nameof(pageNumber),
                    message: "Page number must be greater than 0.");
            if (pageSize is < 1 or > MaxPageSize)
                throw new ArgumentOutOfRangeException(paramName: nameof(pageSize),
                    message: $"Page size must be between 1 and {MaxPageSize}.");

            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        /// <summary>
        ///     Provides a default pagination configuration (page 1, 100 items per page).
        /// </summary>
        public static Pagination Default => new(pageNumber: 1, pageSize: 100);

        /// <summary>Gets the number of the page to retrieve. Must be greater than 0.</summary>
        public int PageNumber { get; init; }

        /// <summary>
        ///     Gets the number of items to include in a single page.
        ///     The value must be between 1 and <see cref="MaxPageSize" />.
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        ///     Calculates the number of records to skip based on the current <see cref="PageNumber" />.
        /// </summary>
        /// <remarks>
        ///     This value is typically used in LINQ queries with <c>Skip()</c> to retrieve
        ///     the correct page of data from the source.
        /// </remarks>
        public int Skip => (PageNumber - 1) * PageSize;

        /// <summary>
        ///     Deconstructs the current <see cref="Pagination" /> instance into its components.
        /// </summary>
        public void Deconstruct(out int pageNumber, out int pageSize)
        {
            pageNumber = PageNumber;
            pageSize = PageSize;
        }
    }
}
