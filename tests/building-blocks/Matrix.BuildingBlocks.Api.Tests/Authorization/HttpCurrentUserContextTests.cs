using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Matrix.BuildingBlocks.Api.Tests.Authorization;

public sealed class HttpCurrentUserContextTests
{
    [Fact]
    public void Constructor_WhenAuthenticatedUserHasStandardClaims_MapsUserAndSessionIds()
    {
        Guid userId = Guid.Parse("9d33dfc1-7340-410c-a776-5fdcb3f67201");
        Guid sessionId = Guid.Parse("e5fc21b9-c124-447e-baef-1e67b5932ea7");
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtClaimNames.SessionId, sessionId.ToString())
        ], authenticationType: "Bearer"));

        HttpCurrentUserContext context = new(new HttpContextAccessor { HttpContext = httpContext });

        Assert.True(context.IsAuthenticated);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(sessionId, context.SessionId);
    }

    [Fact]
    public void Constructor_WhenClaimsUseFallbackTypes_MapsIdentifiers()
    {
        Guid userId = Guid.Parse("39dfb535-d44e-40b7-b611-2804aab93c4c");
        Guid sessionId = Guid.Parse("1dd3d108-d15f-4604-a625-526d7c8d9ca8");
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Sid, sessionId.ToString())
        ], authenticationType: "Cookies"));

        HttpCurrentUserContext context = new(new HttpContextAccessor { HttpContext = httpContext });

        Assert.True(context.IsAuthenticated);
        Assert.Equal(userId, context.UserId);
        Assert.Equal(sessionId, context.SessionId);
    }

    [Fact]
    public void Constructor_WhenUserIsMissingOrClaimsAreInvalid_ReturnsAnonymousContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"),
            new Claim(JwtClaimNames.SessionId, "still-not-a-guid")
        ]));

        HttpCurrentUserContext context = new(new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.SessionId);
    }
}
