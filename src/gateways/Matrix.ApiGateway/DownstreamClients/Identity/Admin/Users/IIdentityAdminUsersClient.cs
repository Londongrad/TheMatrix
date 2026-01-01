using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users
{
    public interface IIdentityAdminUsersClient
    {
        Task<PagedResult<UserListItemResponse>> GetUsersPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        Task<UserDetailsResponse> GetUserDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task LockUserAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task UnlockUserAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task AssignUserRolesAsync(
            Guid userId,
            AssignUserRolesRequest request,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<UserPermissionResponse>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task GrantUserPermissionAsync(
            Guid userId,
            UserPermissionRequest request,
            CancellationToken cancellationToken);

        Task DepriveUserPermissionAsync(
            Guid userId,
            UserPermissionRequest request,
            CancellationToken cancellationToken);
    }
}
