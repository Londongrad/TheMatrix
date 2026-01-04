using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;

namespace Matrix.Identity.Application.Services.SecurityState
{
    /// <summary>
    ///     Default in-memory collector for user security state changes.
    ///     Scoped lifetime. Not thread-safe.
    /// </summary>
    public sealed class SecurityStateChangeCollector : ISecurityStateChangeCollector
    {
        private readonly HashSet<Guid> _changedUsers = new();

        /// <inheritdoc />
        public void MarkUserChanged(Guid userId)
        {
            if (userId == Guid.Empty)
                throw ApplicationErrorsFactory.EmptyId(nameof(userId));

            _changedUsers.Add(userId);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<Guid> DrainUsers()
        {
            if (_changedUsers.Count == 0)
                return Array.Empty<Guid>();

            Guid[] snapshot = _changedUsers.ToArray();
            _changedUsers.Clear();
            return snapshot;
        }
    }
}
