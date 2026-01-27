namespace Matrix.Identity.Application.Abstractions.Services.Administration
{
    public interface IAdminUserGuard
    {
        Task EnsureUserCanBeManagedAsync(
            Guid targetUserId,
            CancellationToken cancellationToken);

        Task EnsureRoleAssignmentIsAllowedAsync(
            IReadOnlyCollection<Guid> desiredRoleIds,
            CancellationToken cancellationToken);
    }
}