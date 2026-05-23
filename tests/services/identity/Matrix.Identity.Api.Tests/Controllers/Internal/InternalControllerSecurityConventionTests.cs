using Matrix.Identity.Api.Authorization.Internal;
using Matrix.Identity.Api.Controllers.Internal;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Identity.Api.Tests.Controllers.Internal;

public sealed class InternalControllerSecurityConventionTests
{
    [Fact]
    public void InternalControllers_RequireInternalApiKeyAttribute()
    {
        Type[] controllerTypes = typeof(InternalUsersController)
            .Assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                string.Equals(
                    type.Namespace,
                    "Matrix.Identity.Api.Controllers.Internal",
                    StringComparison.Ordinal))
            .OrderBy(
                keySelector: type => type.Name,
                comparer: StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(controllerTypes);

        foreach (Type controllerType in controllerTypes)
            Assert.True(
                controllerType.IsDefined(
                    attributeType: typeof(RequireInternalApiKeyAttribute),
                    inherit: true),
                $"{controllerType.FullName} must be decorated with [RequireInternalApiKey].");
    }
}
