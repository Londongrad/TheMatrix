using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles
{
    public interface IIdentityAdminRolesClient
    {
        Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);

        Task<RoleResponse> CreateRoleAsync(
            CreateRoleRequest request,
            CancellationToken cancellationToken);

        Task<RoleResponse> RenameRoleAsync(
            Guid roleId,
            RenameRoleRequest request,
            CancellationToken cancellationToken);

        Task DeleteRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken);

        Task<RolePermissionsResponse> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken);

        Task UpdateRolePermissionsAsync(
            Guid roleId,
            UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken);

        Task<PagedResult<UserListItemResponse>> GetRoleMembersPageAsync(
            Guid roleId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);
    }
}
