using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Mappers;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;
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
        public async Task<ActionResult<IReadOnlyCollection<RoleResponse>>> GetRoles(
            CancellationToken cancellationToken = default)
        {
            var query = new GetRolesListQuery();

            IReadOnlyCollection<RoleListItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            return Ok(result.Select(x => x.ToResponse()));
        }

        [HttpPost("roles")]
        public async Task<ActionResult<RoleResponse>> CreateRole(
            [FromBody] CreateRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new CreateRoleCommand(request.Name);

            RoleCreatedResult result = await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return Ok(result.ToResponse());
        }

        [HttpGet("roles/{roleId:guid}/permissions")]
        public async Task<ActionResult<RolePermissionsResponse>> GetRolePermissions(
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetRolePermissionsQuery(roleId);

            IReadOnlyCollection<string> permissions = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = new RolePermissionsResponse
            {
                PermissionKeys = permissions
            };

            return Ok(response);
        }

        [HttpPut("roles/{roleId:guid}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(
            [FromRoute] Guid roleId,
            [FromBody] UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateRolePermissionsCommand(
                RoleId: roleId,
                RolePermissionKeys: request.PermissionKeys);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("roles/{roleId:guid}/users")]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetRoleMembersPage(
            [FromRoute] Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(
                pageNumber: pageNumber,
                pageSize: pageSize);
            var query = new GetRoleMembersPageQuery(
                RoleId: roleId,
                Pagination: pagination);

            PagedResult<UserListItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var mapped = new PagedResult<UserListItemResponse>(
                items: result.Items.Select(u => new UserListItemResponse
                    {
                        Id = u.Id,
                        AvatarUrl = u.AvatarUrl,
                        Email = u.Email,
                        Username = u.Username,
                        IsEmailConfirmed = u.IsEmailConfirmed,
                        IsLocked = u.IsLocked,
                        CreatedAtUtc = u.CreatedAtUtc
                    })
                   .ToList(),
                totalCount: result.TotalCount,
                pageNumber: result.PageNumber,
                pageSize: result.PageSize);

            return Ok(mapped);
        }

        [HttpGet("permissions")]
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
