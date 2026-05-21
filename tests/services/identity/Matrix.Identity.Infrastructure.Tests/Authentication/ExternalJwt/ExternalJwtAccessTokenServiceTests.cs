using System.IdentityModel.Tokens.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Authentication.ExternalJwt;

public sealed class ExternalJwtAccessTokenServiceTests
{
    [Fact]
    public void Generate_EmitsSignedJwtWithExpectedClaimsAndLifetime()
    {
        DateTimeOffset utcNow = new(2048, 5, 9, 8, 45, 0, TimeSpan.Zero);
        var service = new ExternalJwtAccessTokenService(
            options: CreateJwtOptions(),
            timeProvider: CreateTimeProvider(utcNow));
        Guid userId = Guid.Parse("73054653-d24d-4ee0-98c1-60b003f06495");
        Guid sessionId = Guid.Parse("8868cdae-c44c-4cb2-b8d7-9af85a10f665");

        var tokenModel = service.Generate(userId, permissionsVersion: 7, sessionId: sessionId);
        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(tokenModel.Token);

        Assert.Equal("Bearer", tokenModel.TokenType);
        Assert.Equal(1800, tokenModel.ExpiresInSeconds);
        Assert.Equal("matrix", token.Issuer);
        Assert.Equal("matrix-clients", Assert.Single(token.Audiences));
        Assert.Equal(userId.ToString(), token.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("7", token.Claims.Single(x => x.Type == JwtClaimNames.PermissionsVersion).Value);
        Assert.Equal(sessionId.ToString(), token.Claims.Single(x => x.Type == JwtClaimNames.SessionId).Value);
        Assert.Equal(utcNow.UtcDateTime.AddMinutes(30), token.ValidTo);
    }
}
