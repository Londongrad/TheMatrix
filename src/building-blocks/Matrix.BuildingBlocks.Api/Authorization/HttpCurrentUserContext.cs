using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Matrix.BuildingBlocks.Api.Authorization
{
    public sealed class HttpCurrentUserContext : ICurrentUserContext
    {
        public HttpCurrentUserContext(IHttpContextAccessor accessor)
        {
            ClaimsPrincipal? user = accessor.HttpContext?.User;

            IsAuthenticated = user?.Identity?.IsAuthenticated == true;
            UserId = TryGetUserId(user);
        }

        public bool IsAuthenticated { get; }
        public Guid? UserId { get; }

        private static Guid? TryGetUserId(ClaimsPrincipal? user)
        {
            if (user is null)
                return null;

            string? sub =
                user.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(
                input: sub,
                result: out Guid id)
                ? id
                : null;
        }
    }
}
