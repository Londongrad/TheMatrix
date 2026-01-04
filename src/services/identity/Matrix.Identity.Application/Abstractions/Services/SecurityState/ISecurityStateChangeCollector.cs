namespace Matrix.Identity.Application.Abstractions.Services.SecurityState
{
    /// <summary>
    ///     Collects user IDs whose security state has changed within the current scope.
    /// </summary>
    public interface ISecurityStateChangeCollector
    {
        /// <summary>
        ///     Marks the user as having a security-related change (roles, permissions, lock state).
        /// </summary>
        void MarkUserChanged(Guid userId);

        /// <summary>
        ///     Drains all collected user IDs and clears the internal buffer.
        /// </summary>
        IReadOnlyCollection<Guid> DrainUsers();
    }
}
