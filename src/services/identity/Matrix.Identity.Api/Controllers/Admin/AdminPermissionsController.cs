using Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;
using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Admin
{
    [ApiController]
    [Authorize]
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

        [HttpGet("default-user-access")]
        public async Task<ActionResult<DefaultUserAccessPermissionsResponse>> GetDefaultUserAccessPermissions(
            CancellationToken cancellationToken = default)
        {
            var query = new GetDefaultUserAccessPermissionsQuery();

            DefaultUserAccessPermissionsResult result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            return Ok(
                new DefaultUserAccessPermissionsResponse
                {
                    Version = result.Version,
                    PermissionKeys = result.PermissionKeys
                });
        }

        [HttpPut("default-user-access")]
        public async Task<IActionResult> UpdateDefaultUserAccessPermissions(
            [FromBody] UpdateDefaultUserAccessPermissionsRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateDefaultUserAccessPermissionsCommand(request.PermissionKeys);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
