using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Internal
{
    [ApiController]
    [Route("api/internal/users")]
    public sealed class InternalUsersController(IUserRepository userRepository) : ControllerBase
    {
        /// <summary>
        ///     Returns the current permissions version for the specified user.
        /// </summary>
        [HttpGet("{userId:guid}/permissions-version")]
        public async Task<ActionResult<PermissionsVersionResponse>> GetPermissionsVersion(
            Guid userId,
            CancellationToken cancellationToken)
        {
            int? version = await userRepository.GetPermissionsVersionAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            if (version is null)
                return NotFound();

            return Ok(new PermissionsVersionResponse(version.Value));
        }
    }
}
