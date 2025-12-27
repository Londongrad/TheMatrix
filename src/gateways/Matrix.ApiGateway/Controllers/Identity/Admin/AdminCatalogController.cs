using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Catalog;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
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
        public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(CancellationToken ct)
        {
            IReadOnlyCollection<RoleResponse> roles = await _catalogClient.GetRolesAsync(ct);
            return Ok(roles);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>>> GetPermissions(
            CancellationToken ct)
        {
            IReadOnlyCollection<PermissionCatalogItemResponse> permissions =
                await _catalogClient.GetPermissionsAsync(ct);

            return Ok(permissions);
        }
    }
}
