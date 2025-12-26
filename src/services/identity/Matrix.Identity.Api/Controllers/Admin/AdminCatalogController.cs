using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin")]
    public class AdminCatalogController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("roles")]
        [Authorize(Policy = PermissionKeys.IdentityRolesList)]
        public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(
            CancellationToken cancellationToken = default)
        {
            var query = new GetRolesListQuery();

            IReadOnlyCollection<RoleListItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            List<RoleResponse> response = result
               .Select(role => new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    IsSystem = role.IsSystem,
                    CreatedAtUtc = role.CreatedAtUtc
                })
               .ToList();

            return Ok(response);
        }

        [HttpGet("permissions")]
        [Authorize(Policy = PermissionKeys.IdentityPermissionsCatalogRead)]
        public async Task<ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>>> GetPermissions(
            CancellationToken cancellationToken = default)
        {
            var query = new GetPermissionsCatalogQuery();

            IReadOnlyCollection<PermissionCatalogItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            List<PermissionCatalogItemResponse> response = result
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
