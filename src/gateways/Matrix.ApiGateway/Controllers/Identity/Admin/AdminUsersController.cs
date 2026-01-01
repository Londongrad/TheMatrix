using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize]
    public sealed class AdminUsersController(IIdentityAdminUsersClient usersClient) : ControllerBase
    {
        private readonly IIdentityAdminUsersClient _usersClient = usersClient;

        [HttpGet]
        public async Task<ActionResult<PagedResult<UserListItemResponse>>> GetUsersPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            PagedResult<UserListItemResponse> page = await _usersClient.GetUsersPageAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: cancellationToken);

            // Нормализуем AvatarUrl как в AccountController
            foreach (UserListItemResponse item in page.Items)
                item.AvatarUrl = ToPublicAvatarUrl(item.AvatarUrl);

            return Ok(page);
        }

        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<UserDetailsResponse>> GetUserDetails(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            UserDetailsResponse dto = await _usersClient.GetUserDetailsAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            dto.AvatarUrl = ToPublicAvatarUrl(dto.AvatarUrl);

            return Ok(dto);
        }

        [HttpPost("{userId:guid}/lock")]
        public async Task<IActionResult> LockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            await _usersClient.LockUserAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{userId:guid}/unlock")]
        public async Task<IActionResult> UnlockUser(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            await _usersClient.UnlockUserAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("{userId:guid}/roles")]
        public async Task<ActionResult<IReadOnlyCollection<UserRoleResponse>>> GetUserRoles(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<UserRoleResponse> roles = await _usersClient.GetUserRolesAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            return Ok(roles);
        }

        [HttpPut("{userId:guid}/roles")]
        public async Task<IActionResult> AssignUserRoles(
            [FromRoute] Guid userId,
            [FromBody] AssignUserRolesRequest request,
            CancellationToken cancellationToken)
        {
            await _usersClient.AssignUserRolesAsync(
                userId: userId,
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("{userId:guid}/permissions")]
        public async Task<ActionResult<IReadOnlyCollection<UserPermissionResponse>>> GetUserPermissions(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<UserPermissionResponse> perms =
                await _usersClient.GetUserPermissionsAsync(
                    userId: userId,
                    cancellationToken: cancellationToken);

            return Ok(perms);
        }

        [HttpPost("{userId:guid}/permissions/grant")]
        public async Task<IActionResult> GrantUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken)
        {
            await _usersClient.GrantUserPermissionAsync(
                userId: userId,
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{userId:guid}/permissions/deprive")]
        public async Task<IActionResult> DepriveUserPermission(
            [FromRoute] Guid userId,
            [FromBody] UserPermissionRequest request,
            CancellationToken cancellationToken)
        {
            await _usersClient.DepriveUserPermissionAsync(
                userId: userId,
                request: request,
                cancellationToken: cancellationToken);

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
