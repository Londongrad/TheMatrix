using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin/roles")]
    public sealed class AdminRolesController(IIdentityAdminRolesClient rolesClient) : ControllerBase
    {
        private readonly IIdentityAdminRolesClient _rolesClient = rolesClient;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<RoleResponse> roles = await _rolesClient.GetRolesAsync(cancellationToken);
            return Ok(roles);
        }

        [HttpPost]
        public async Task<ActionResult<RoleResponse>> CreateRole(
            [FromBody] CreateRoleRequest request,
            CancellationToken cancellationToken)
        {
            RoleResponse role = await _rolesClient.CreateRoleAsync(
                request: request,
                cancellationToken: cancellationToken);

            return Ok(role);
        }

        [HttpPut("{roleId:guid}")]
        public async Task<ActionResult<RoleResponse>> RenameRole(
            [FromRoute] Guid roleId,
            [FromBody] RenameRoleRequest request,
            CancellationToken cancellationToken)
        {
            RoleResponse role = await _rolesClient.RenameRoleAsync(
                roleId: roleId,
                request: request,
                cancellationToken: cancellationToken);

            return Ok(role);
        }

        [HttpDelete("{roleId:guid}")]
        public async Task<IActionResult> DeleteRole(
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            await _rolesClient.DeleteRoleAsync(
                roleId: roleId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("{roleId:guid}/permissions")]
        public async Task<ActionResult<RolePermissionsResponse>> GetRolePermissions(
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            RolePermissionsResponse permissions =
                await _rolesClient.GetRolePermissionsAsync(
                    roleId: roleId,
                    cancellationToken: cancellationToken);

            return Ok(permissions);
        }

        [HttpPut("{roleId:guid}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(
            [FromRoute] Guid roleId,
            [FromBody] UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            await _rolesClient.UpdateRolePermissionsAsync(
                roleId: roleId,
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("{roleId:guid}/users")]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetRoleMembersPage(
            [FromRoute] Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<UserListItemResponse> result =
                await _rolesClient.GetRoleMembersPageAsync(
                    roleId: roleId,
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
