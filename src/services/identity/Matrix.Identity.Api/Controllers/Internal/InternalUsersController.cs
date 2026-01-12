using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Internal
{
    [ApiController]
    [Route("api/internal/users/{userId:guid}")]
    public sealed class InternalUsersController(
        IUserRepository userRepository,
        IEffectivePermissionsService effectivePermissionsService) : ControllerBase
    {
        /// <summary>
        ///     Returns the current permissions version for the specified user.
        /// </summary>
        [HttpGet("permissions-version")]
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

        /// <summary>
        ///     Returns permissions version + effective permissions for the specified user.
        ///     Used by API Gateway to cache and issue internal JWT.
        /// </summary>
        [HttpGet("auth-context")]
        public async Task<ActionResult<UserAuthContextResponse>> GetAuthContext(
            Guid userId,
            CancellationToken cancellationToken)
        {
            // Самый простой и достаточно "взрослый" вариант:
            // 1) посчитать через existing service (DB)
            // 2) отдать gateway (он уже решит, как кэшировать)
            try
            {
                AuthorizationContext ctx = await effectivePermissionsService.GetAuthContextAsync(
                    userId: userId,
                    cancellationToken: cancellationToken);

                string[] permissions = ctx.Permissions
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Distinct(StringComparer.Ordinal)
                   .OrderBy(
                        keySelector: p => p,
                        comparer: StringComparer.Ordinal)
                   .ToArray();

                return Ok(
                    new UserAuthContextResponse(
                        PermissionsVersion: ctx.PermissionsVersion,
                        EffectivePermissions: permissions));
            }
            catch (InvalidOperationException)
            {
                // EffectivePermissionsService делает SingleAsync по Users (если юзера нет — будет InvalidOperationException)
                return NotFound();
            }
        }
    }
}
