using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Matrix.BuildingBlocks.Api.Tests.Authorization
{
    public sealed class HttpCurrentUserContextTests
    {
        [Fact]
        public void Constructor_WhenAuthenticatedUserHasStandardClaims_MapsUserAndSessionIds()
        {
            var userId = Guid.Parse("9d33dfc1-7340-410c-a776-5fdcb3f67201");
            var sessionId = Guid.Parse("e5fc21b9-c124-447e-baef-1e67b5932ea7");
            DefaultHttpContext httpContext = new();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtRegisteredClaimNames.Sub,
                            value: userId.ToString()),
                        new Claim(
                            type: JwtClaimNames.SessionId,
                            value: sessionId.ToString())
                    ],
                    authenticationType: "Bearer"));

            HttpCurrentUserContext context = new(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                });

            Assert.True(context.IsAuthenticated);
            Assert.Equal(
                expected: userId,
                actual: context.UserId);
            Assert.Equal(
                expected: sessionId,
                actual: context.SessionId);
        }

        [Fact]
        public void Constructor_WhenClaimsUseFallbackTypes_MapsIdentifiers()
        {
            var userId = Guid.Parse("39dfb535-d44e-40b7-b611-2804aab93c4c");
            var sessionId = Guid.Parse("1dd3d108-d15f-4604-a625-526d7c8d9ca8");
            DefaultHttpContext httpContext = new();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: ClaimTypes.NameIdentifier,
                            value: userId.ToString()),
                        new Claim(
                            type: ClaimTypes.Sid,
                            value: sessionId.ToString())
                    ],
                    authenticationType: "Cookies"));

            HttpCurrentUserContext context = new(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                });

            Assert.True(context.IsAuthenticated);
            Assert.Equal(
                expected: userId,
                actual: context.UserId);
            Assert.Equal(
                expected: sessionId,
                actual: context.SessionId);
        }

        [Fact]
        public void Constructor_WhenUserIsMissingOrClaimsAreInvalid_ReturnsAnonymousContext()
        {
            DefaultHttpContext httpContext = new();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(
                        type: JwtRegisteredClaimNames.Sub,
                        value: "not-a-guid"),
                    new Claim(
                        type: JwtClaimNames.SessionId,
                        value: "still-not-a-guid")
                ]));

            HttpCurrentUserContext context = new(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                });

            Assert.False(context.IsAuthenticated);
            Assert.Null(context.UserId);
            Assert.Null(context.SessionId);
        }
    }
}
