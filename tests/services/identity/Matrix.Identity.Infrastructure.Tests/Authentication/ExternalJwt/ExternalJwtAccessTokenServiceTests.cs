using System.IdentityModel.Tokens.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Identity.Application.UseCases.Self.Auth;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Authentication.ExternalJwt
{
    public sealed class ExternalJwtAccessTokenServiceTests
    {
        [Fact]
        public void Generate_EmitsSignedJwtWithExpectedClaimsAndLifetime()
        {
            DateTimeOffset utcNow = new(
                year: 2048,
                month: 5,
                day: 9,
                hour: 8,
                minute: 45,
                second: 0,
                offset: TimeSpan.Zero);
            var service = new ExternalJwtAccessTokenService(
                options: CreateJwtOptions(),
                timeProvider: CreateTimeProvider(utcNow));
            var userId = Guid.Parse("73054653-d24d-4ee0-98c1-60b003f06495");
            var sessionId = Guid.Parse("8868cdae-c44c-4cb2-b8d7-9af85a10f665");

            AccessTokenModel tokenModel = service.Generate(
                userId: userId,
                permissionsVersion: 7,
                sessionId: sessionId);
            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenModel.Token);

            Assert.Equal(
                expected: "Bearer",
                actual: tokenModel.TokenType);
            Assert.Equal(
                expected: 1800,
                actual: tokenModel.ExpiresInSeconds);
            Assert.Equal(
                expected: "matrix",
                actual: token.Issuer);
            Assert.Equal(
                expected: "matrix-clients",
                actual: Assert.Single(token.Audiences));
            Assert.Equal(
                expected: userId.ToString(),
                actual: token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub)
                   .Value);
            Assert.Equal(
                expected: "7",
                actual: token.Claims.Single(x => x.Type == JwtClaimNames.PermissionsVersion)
                   .Value);
            Assert.Equal(
                expected: sessionId.ToString(),
                actual: token.Claims.Single(x => x.Type == JwtClaimNames.SessionId)
                   .Value);
            Assert.Equal(
                expected: utcNow.UtcDateTime.AddMinutes(30),
                actual: token.ValidTo);
        }
    }
}
