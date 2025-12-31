using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.AssignUserRoles;
using Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/admin/users")]
    public class AdminUsersController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        [Authorize(Policy = PermissionKeys.IdentityUsersList)]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsersPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(
                pageNumber: pageNumber,
                pageSize: pageSize);

            var query = new GetUsersPageQuery(pagination);

            PagedResult<UserListItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            List<UserListItemResponse> items = result.Items
               .Select(u => new UserListItemResponse
                {
                    Id = u.Id,
                    AvatarUrl = u.AvatarUrl,
                    Email = u.Email,
                    Username = u.Username,
                    IsEmailConfirmed = u.IsEmailConfirmed,
                    IsLocked = u.IsLocked,
                    CreatedAtUtc = u.CreatedAtUtc
                })
               .ToList();

            var response = new PagedResult<UserListItemResponse>(
                items: items,
                totalCount: result.TotalCount,
                pageNumber: result.PageNumber,
                pageSize: result.PageSize);

            return Ok(response);
        }

        [HttpGet("{userId:guid}")]
        [Authorize(Policy = PermissionKeys.IdentityUsersRead)]
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

        [HttpPost("{userId:guid}/lock")]
        [Authorize(Policy = PermissionKeys.IdentityUsersLock)]
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
        [Authorize(Policy = PermissionKeys.IdentityUsersUnlock)]
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

        [HttpGet("{userId:guid}/roles")]
        [Authorize(Policy = PermissionKeys.IdentityUserRolesRead)]
        public async Task<ActionResult<IReadOnlyCollection<UserRoleResponse>>> GetUserRoles(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserRolesQuery(userId);

            IReadOnlyCollection<UserRoleResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            List<UserRoleResponse> response = result
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
        [Authorize(Policy = PermissionKeys.IdentityUserRolesAssign)]
        public async Task<IActionResult> AssignUserRoles(
            [FromRoute] Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new AssignUserRolesCommand(
                UserId: userId,
                RoleIds: request.RoleIds);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("{userId:guid}/permissions")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsRead)]
        public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponse>>> GetUserPermissions(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetUserPermissionsQuery(userId);

            IReadOnlyCollection<UserPermissionOverrideResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            List<UserPermissionResponse> response = result
               .Select(permission => new UserPermissionResponse
                {
                    PermissionKey = permission.PermissionKey,
                    Effect = permission.Effect.ToString()
                })
               .ToList();

            return Ok(response);
        }

        [HttpPost("{userId:guid}/permissions/grant")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsGrant)]
        public async Task<IActionResult> GrantUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new GrantUserPermissionCommand(
                UserId: userId,
                PermissionKey: request.PermissionKey);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{userId:guid}/permissions/deprive")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsDeprive)]
        public async Task<IActionResult> DepriveUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new DepriveUserPermissionCommand(
                UserId: userId,
                PermissionKey: request.PermissionKey);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
