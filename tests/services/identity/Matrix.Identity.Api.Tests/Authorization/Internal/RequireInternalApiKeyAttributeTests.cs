using Matrix.Identity.Api.Authorization.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Matrix.Identity.Api.Tests.Authorization.Internal;

public sealed class RequireInternalApiKeyAttributeTests
{
    [Fact]
    public void OnAuthorization_WhenContextIsTrusted_AllowsRequest()
    {
        var attribute = new RequireInternalApiKeyAttribute();
        AuthorizationFilterContext context = CreateFilterContext(trusted: true);

        attribute.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenContextIsNotTrusted_ReturnsUnauthorized()
    {
        var attribute = new RequireInternalApiKeyAttribute();
        AuthorizationFilterContext context = CreateFilterContext(trusted: false);

        attribute.OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    private static AuthorizationFilterContext CreateFilterContext(bool trusted)
    {
        DefaultHttpContext httpContext = new();

        if (trusted)
            TrustedGatewayRequestContext.Mark(httpContext);

        var actionContext = new ActionContext(
            httpContext: httpContext,
            routeData: new RouteData(),
            actionDescriptor: new ActionDescriptor());

        return new AuthorizationFilterContext(
            actionContext: actionContext,
            filters: []);
    }
}
