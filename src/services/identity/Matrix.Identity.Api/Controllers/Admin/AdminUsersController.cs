using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Authorization.Permissions;
using Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = PermissionKeys.IdentityAdminAccess)]
    public sealed class AdminUsersController(ISender sender) : ControllerBase
    {
        #region [ Fields ]

        private readonly ISender _sender = sender;

        #endregion [ Fields ]

        #region [ Get users ]

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsersPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(
                pageNumber: pageNumber,
                pageSize: pageSize);

            var query = new GetUsersPageQuery(pagination);

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

        #endregion [ Get users ]

        #region [ Get user details ]

        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<UserDetailsResponse>> GetUserDetails(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserDetailsQuery(userId);

            UserDetailsResult result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = new UserDetailsResponse
            {
                Id = result.Id,
                AvatarUrl = result.AvatarUrl,
                Username = result.Username,
                Email = result.Email,
                IsEmailConfirmed = result.IsEmailConfirmed,
                IsLocked = result.IsLocked,
                PermissionsVersion = result.PermissionsVersion,
                CreatedAtUtc = result.CreatedAtUtc
            };

            return Ok(response);
        }

        #endregion [ Get user details ]

        #region [ Lock/Unlock user ]

        [HttpPost("{userId:guid}/lock")]
        public async Task<IActionResult> LockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var command = new LockUserCommand(userId);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{userId:guid}/unlock")]
        public async Task<IActionResult> UnlockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var command = new UnlockUserCommand(userId);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Lock/Unlock user ]

        #region [ Roles ]

        [HttpGet("{userId:guid}/roles")]
        public async Task<ActionResult<IReadOnlyCollection<UserRoleResponse>>> GetUserRoles(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserRolesQuery(userId);

            IReadOnlyCollection<UserRoleResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = result
               .Select(role => new UserRoleResponse
                {
                    Id = role.Id,
                    Name = role.Name,
                    IsSystem = role.IsSystem,
                    CreatedAtUtc = role.CreatedAtUtc
                })
               .ToList();

            return Ok(response);
        }

        [HttpPut("{userId:guid}/roles")]
        public async Task<IActionResult> AssignUserRoles(
            [FromRoute] Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateUserRolesCommand(
                UserId: userId,
                RoleIds: request.RoleIds);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Roles ]

        #region [ Permissions ]

        [HttpGet("{userId:guid}/permissions")]
        public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponse>>> GetUserPermissions(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserPermissionsQuery(userId);

            IReadOnlyCollection<UserPermissionOverrideResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = result
               .Select(permission => new UserPermissionResponse
                {
                    PermissionKey = permission.PermissionKey,
                    Effect = permission.Effect.ToString()
                })
               .ToList();

            return Ok(response);
        }

        [HttpPost("{userId:guid}/permissions/grant")]
        public async Task<IActionResult> GrantUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new GrantUserPermissionCommand(
                UserId: userId,
                TargetPermissionKey: request.PermissionKey);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{userId:guid}/permissions/deprive")]
        public async Task<IActionResult> DepriveUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new DepriveUserPermissionCommand(
                UserId: userId,
                TargetPermissionKey: request.PermissionKey);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Permissions ]
    }
}
