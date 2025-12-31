using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Catalog;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin")]
    public sealed class AdminCatalogController(IIdentityAdminCatalogClient catalogClient) : ControllerBase
    {
        private readonly IIdentityAdminCatalogClient _catalogClient = catalogClient;

        [HttpGet("roles")]
        public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RoleResponse> roles = await _catalogClient.GetRolesAsync(cancellationToken);
            return Ok(roles);
        }

        [HttpPost("roles")]
        public async Task<ActionResult<RoleResponse>> CreateRole(
            [FromBody] CreateRoleRequest request,
            CancellationToken cancellationToken)
        {
            RoleResponse role = await _catalogClient.CreateRoleAsync(request, cancellationToken);
            return Ok(role);
        }

        [HttpGet("roles/{roleId:guid}/permissions")]
        public async Task<ActionResult<RolePermissionsResponse>> GetRolePermissions(
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            RolePermissionsResponse permissions =
                await _catalogClient.GetRolePermissionsAsync(roleId, cancellationToken);
            return Ok(permissions);
        }

        [HttpPut("roles/{roleId:guid}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(
            [FromRoute] Guid roleId,
            [FromBody] UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            await _catalogClient.UpdateRolePermissionsAsync(roleId, request, cancellationToken);
            return NoContent();
        }

        [HttpGet("roles/{roleId:guid}/users")]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetRoleMembersPage(
            [FromRoute] Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<UserListItemResponse> result =
                await _catalogClient.GetRoleMembersPageAsync(roleId, pageNumber, pageSize, cancellationToken);

            return Ok(result);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>>> GetPermissions(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<PermissionCatalogItemResponse> permissions =
                await _catalogClient.GetPermissionsAsync(cancellationToken);

            return Ok(permissions);
        }
    }
}
