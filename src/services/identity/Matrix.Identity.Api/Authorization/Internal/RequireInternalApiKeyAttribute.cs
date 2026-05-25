using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Matrix.Identity.Api.Authorization.Internal
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireInternalApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (TrustedGatewayRequestContext.IsTrusted(context.HttpContext))
                return;

            context.Result = new UnauthorizedResult();
        }
    }
}
