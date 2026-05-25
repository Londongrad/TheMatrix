using System.IdentityModel.Tokens.Jwt;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Authorization.InternalJwt
{
    public sealed class InternalJwtIssuerTests
    {
        [Fact]
        public void Issue_WhenCalled_EmitsExpectedClaimsHeaderAndLifetime()
        {
            DateTimeOffset utcNow = new(
                year: 2046,
                month: 4,
                day: 17,
                hour: 9,
                minute: 30,
                second: 0,
                offset: TimeSpan.Zero);
            var issuer = new InternalJwtIssuer(
                options: CreateInternalJwtOptions(lifetimeSeconds: 90),
                timeProvider: CreateTimeProvider(utcNow));
            var userId = Guid.Parse("f2ab912e-aeb4-49fe-a28e-5d991af454d7");

            string tokenText = issuer.Issue(
                userId: userId,
                jti: "gateway-jti",
                permissionsVersion: 7,
                permissions:
                [
                    "city.write",
                    "city.read",
                    "city.write",
                    "",
                    "  ",
                    "city.admin"
                ]);

            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

            Assert.Equal(
                expected: "matrix-gateway",
                actual: token.Issuer);
            Assert.Equal(
                expected: "matrix-internal",
                actual: Assert.Single(token.Audiences));
            Assert.Equal(
                expected: "kid-current",
                actual: token.Header.Kid);
            Assert.Equal(
                expected: userId.ToString(),
                actual: token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub)
                   .Value);
            Assert.Equal(
                expected: "gateway-jti",
                actual: token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti)
                   .Value);
            Assert.Equal(
                expected: InternalJwtTokenKinds.UserContext,
                actual: token.Claims.Single(x => x.Type == JwtClaimNames.InternalTokenKind)
                   .Value);
            Assert.Equal(
                expected: "7",
                actual: token.Claims.Single(x => x.Type == JwtClaimNames.PermissionsVersion)
                   .Value);
            Assert.Equal(
                expectedSpan:
                [
                    "city.admin",
                    "city.read",
                    "city.write"
                ],
                actualArray: token.Claims.Where(x => x.Type == JwtClaimNames.Permission)
                   .Select(x => x.Value)
                   .ToArray());
            Assert.Equal(
                expected: utcNow.AddSeconds(90)
                   .UtcDateTime,
                actual: token.ValidTo);
        }

        [Fact]
        public void Issue_WhenJtiIsMissing_GeneratesGuidClaim()
        {
            var issuer = new InternalJwtIssuer(
                options: CreateInternalJwtOptions(),
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        year: 2046,
                        month: 4,
                        day: 17,
                        hour: 9,
                        minute: 45,
                        second: 0,
                        offset: TimeSpan.Zero)));

            string tokenText = issuer.Issue(
                userId: Guid.Parse("ee462735-7323-47af-bf45-9ee7ff5f4896"),
                jti: " ",
                permissionsVersion: 3,
                permissions: ["city.read"]);

            JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

            Assert.True(
                Guid.TryParse(
                    input: token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti)
                       .Value,
                    result: out _));
        }
    }
}
