using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users
{
    public interface IIdentityAdminUsersClient
    {
        Task<PagedResult<UserListItemResponse>> GetUsersPageAsync(int pageNumber, int pageSize, CancellationToken ct);
        Task<UserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken ct);

        Task LockUserAsync(Guid userId, CancellationToken ct);
        Task UnlockUserAsync(Guid userId, CancellationToken ct);

        Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesAsync(Guid userId, CancellationToken ct);
        Task AssignUserRolesAsync(Guid userId, AssignUserRolesRequest request, CancellationToken ct);

        Task<IReadOnlyCollection<UserPermissionResponse>> GetUserPermissionsAsync(Guid userId, CancellationToken ct);
        Task GrantUserPermissionAsync(Guid userId, UserPermissionRequest request, CancellationToken ct);
        Task DepriveUserPermissionAsync(Guid userId, UserPermissionRequest request, CancellationToken ct);
    }
}
