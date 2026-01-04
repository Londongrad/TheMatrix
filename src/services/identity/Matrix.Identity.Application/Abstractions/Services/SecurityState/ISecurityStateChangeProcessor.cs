namespace Matrix.Identity.Application.Abstractions.Services.SecurityState
{
    /// <summary>
    ///     Processes collected security state changes (permissions version bump + outbox write).
    /// </summary>
    public interface ISecurityStateChangeProcessor
    {
        /// <summary>
        ///     Persists security state changes for collected users.
        /// </summary>
        Task ProcessAsync(CancellationToken cancellationToken);
    }
}
