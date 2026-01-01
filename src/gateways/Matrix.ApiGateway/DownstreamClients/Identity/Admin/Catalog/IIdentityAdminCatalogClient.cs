using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Catalog
{
    public interface IIdentityAdminCatalogClient
    {
        Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken);

        Task<RoleResponse> CreateRoleAsync(
            CreateRoleRequest request,
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

        Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(
            CancellationToken cancellationToken);
    }
}
