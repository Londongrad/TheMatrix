using Matrix.Identity.Api.Authorization.Internal;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Internal
{
    [RequireInternalApiKey]
    [ApiController]
    [Route("api/internal/authorization")]
    public sealed class InternalAuthorizationController(
        IDefaultUserAccessPolicyRepository defaultUserAccessPolicyRepository)
        : ControllerBase
    {
        [HttpGet("default-user-access/version")]
        public async Task<ActionResult<DefaultUserAccessVersionResponse>> GetDefaultUserAccessVersion(
            CancellationToken cancellationToken)
        {
            int version = await defaultUserAccessPolicyRepository.GetVersionAsync(cancellationToken);
            return Ok(new DefaultUserAccessVersionResponse(version));
        }
    }
}
