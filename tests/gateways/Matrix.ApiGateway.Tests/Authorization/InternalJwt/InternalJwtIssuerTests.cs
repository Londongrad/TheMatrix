using System.IdentityModel.Tokens.Jwt;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Authorization.InternalJwt;

public sealed class InternalJwtIssuerTests
{
    [Fact]
    public void Issue_WhenCalled_EmitsExpectedClaimsHeaderAndLifetime()
    {
        DateTimeOffset utcNow = new(2046, 4, 17, 9, 30, 0, TimeSpan.Zero);
        var issuer = new InternalJwtIssuer(
            options: CreateInternalJwtOptions(lifetimeSeconds: 90),
            timeProvider: CreateTimeProvider(utcNow));
        Guid userId = Guid.Parse("f2ab912e-aeb4-49fe-a28e-5d991af454d7");

        string tokenText = issuer.Issue(
            userId: userId,
            jti: "gateway-jti",
            permissionsVersion: 7,
            permissions: ["city.write", "city.read", "city.write", "", "  ", "city.admin"]);

        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.Equal("matrix-gateway", token.Issuer);
        Assert.Equal("matrix-internal", Assert.Single(token.Audiences));
        Assert.Equal("kid-current", token.Header.Kid);
        Assert.Equal(userId.ToString(), token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("gateway-jti", token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value);
        Assert.Equal(InternalJwtTokenKinds.UserContext, token.Claims.Single(x => x.Type == JwtClaimNames.InternalTokenKind).Value);
        Assert.Equal("7", token.Claims.Single(x => x.Type == JwtClaimNames.PermissionsVersion).Value);
        Assert.Equal(
            ["city.admin", "city.read", "city.write"],
            token.Claims.Where(x => x.Type == JwtClaimNames.Permission).Select(x => x.Value).ToArray());
        Assert.Equal(utcNow.AddSeconds(90).UtcDateTime, token.ValidTo);
    }

    [Fact]
    public void Issue_WhenJtiIsMissing_GeneratesGuidClaim()
    {
        var issuer = new InternalJwtIssuer(
            options: CreateInternalJwtOptions(),
            timeProvider: CreateTimeProvider(new DateTimeOffset(2046, 4, 17, 9, 45, 0, TimeSpan.Zero)));

        string tokenText = issuer.Issue(
            userId: Guid.Parse("ee462735-7323-47af-bf45-9ee7ff5f4896"),
            jti: " ",
            permissionsVersion: 3,
            permissions: ["city.read"]);

        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenText);

        Assert.True(Guid.TryParse(token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value, out _));
    }
}
