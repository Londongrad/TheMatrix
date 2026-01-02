using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin/permissions")]
    public sealed class AdminPermissionsController(IIdentityAdminPermissionsClient permissionsClient) : ControllerBase
    {
        private readonly IIdentityAdminPermissionsClient _permissionsClient = permissionsClient;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>>> GetPermissions(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<PermissionCatalogItemResponse> permissions =
                await _permissionsClient.GetPermissionsAsync(cancellationToken);

            return Ok(permissions);
        }
    }
}
