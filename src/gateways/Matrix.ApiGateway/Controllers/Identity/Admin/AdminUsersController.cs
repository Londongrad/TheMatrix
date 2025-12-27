using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = PermissionKeys.IdentityUsersRead)]
    public sealed class AdminUsersController(IIdentityAdminUsersClient usersClient) : ControllerBase
    {
        private readonly IIdentityAdminUsersClient _usersClient = usersClient;

        [HttpGet]
        [Authorize(Policy = PermissionKeys.IdentityUsersList)]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsersPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            PagedResult<UserListItemResponse> page = await _usersClient.GetUsersPageAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                ct: ct);

            // Нормализуем AvatarUrl как в AccountController
            foreach (UserListItemResponse item in page.Items)
                item.AvatarUrl = ToPublicAvatarUrl(item.AvatarUrl);

            return Ok(page);
        }

        [HttpGet("{userId:guid}")]
        [Authorize(Policy = PermissionKeys.IdentityUsersRead)]
        public async Task<ActionResult<UserDetailsResponse>> GetUserDetails(
            [FromRoute] Guid userId,
            CancellationToken ct)
        {
            UserDetailsResponse dto = await _usersClient.GetUserDetailsAsync(
                userId: userId,
                ct: ct);

            dto.AvatarUrl = ToPublicAvatarUrl(dto.AvatarUrl);

            return Ok(dto);
        }

        [HttpPost("{userId:guid}/lock")]
        [Authorize(Policy = PermissionKeys.IdentityUsersLock)]
        public async Task<IActionResult> LockUser(
            [FromRoute] Guid userId,
            CancellationToken ct)
        {
            await _usersClient.LockUserAsync(
                userId: userId,
                ct: ct);

            return NoContent();
        }

        [HttpPost("{userId:guid}/unlock")]
        [Authorize(Policy = PermissionKeys.IdentityUsersUnlock)]
        public async Task<IActionResult> UnlockUser(
            [FromRoute] Guid userId,
            CancellationToken ct)
        {
            await _usersClient.UnlockUserAsync(
                userId: userId,
                ct: ct);

            return NoContent();
        }

        [HttpGet("{userId:guid}/roles")]
        [Authorize(Policy = PermissionKeys.IdentityUserRolesRead)]
        public async Task<ActionResult<IReadOnlyCollection<UserRoleResponse>>> GetUserRoles(
            [FromRoute] Guid userId,
            CancellationToken ct)
        {
            IReadOnlyCollection<UserRoleResponse> roles = await _usersClient.GetUserRolesAsync(
                userId: userId,
                ct: ct);

            return Ok(roles);
        }

        [HttpPut("{userId:guid}/roles")]
        [Authorize(Policy = PermissionKeys.IdentityUserRolesAssign)]
        public async Task<IActionResult> AssignUserRoles(
            [FromRoute] Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken ct)
        {
            await _usersClient.AssignUserRolesAsync(
                userId: userId,
                request: request,
                ct: ct);

            return NoContent();
        }

        [HttpGet("{userId:guid}/permissions")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsRead)]
        public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponse>>> GetUserPermissions(
            [FromRoute] Guid userId,
            CancellationToken ct)
        {
            IReadOnlyCollection<UserPermissionResponse> perms =
                await _usersClient.GetUserPermissionsAsync(
                    userId: userId,
                    ct: ct);

            return Ok(perms);
        }

        [HttpPost("{userId:guid}/permissions/grant")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsGrant)]
        public async Task<IActionResult> GrantUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken ct)
        {
            await _usersClient.GrantUserPermissionAsync(
                userId: userId,
                request: request,
                ct: ct);

            return NoContent();
        }

        [HttpPost("{userId:guid}/permissions/deprive")]
        [Authorize(Policy = PermissionKeys.IdentityUserPermissionsDeprive)]
        public async Task<IActionResult> DepriveUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken ct)
        {
            await _usersClient.DepriveUserPermissionAsync(
                userId: userId,
                request: request,
                ct: ct);

            return NoContent();
        }

        private string? ToPublicAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            if (Uri.TryCreate(
                    uriString: avatarUrl,
                    uriKind: UriKind.Absolute,
                    result: out _))
                return avatarUrl;

            if (!avatarUrl.StartsWith('/'))
                avatarUrl = "/" + avatarUrl;

            return $"{Request.Scheme}://{Request.Host}{avatarUrl}";
        }
    }
}
