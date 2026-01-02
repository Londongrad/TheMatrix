using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Admin
{
    [ApiController]
    [Authorize(Policy = PermissionKeys.IdentityAdminAccess)]
    [Route("api/admin/permissions")]
    public class AdminPermissionsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>>> GetPermissions(
            CancellationToken cancellationToken = default)
        {
            var query = new GetPermissionsCatalogQuery();

            IReadOnlyCollection<PermissionCatalogItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = result
               .Select(permission => new PermissionCatalogItemResponse
                {
                    Key = permission.Key,
                    Service = permission.Service,
                    Group = permission.Group,
                    Description = permission.Description,
                    IsDeprecated = permission.IsDeprecated
                })
               .ToList();

            return Ok(response);
        }
    }
}
