using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.Authorization;

public sealed class ClaimsAndInternalServicesTests
{
    [Fact]
    public async Task ClaimsPermissionChecker_WhenWildcardExists_AllowsChecksAndCachesByUser()
    {
        Guid userId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtClaimNames.Permission, "*"),
            new Claim(JwtClaimNames.Permission, "users.read")
        ], "test"));

        ClaimsPermissionChecker checker = new(new HttpContextAccessor { HttpContext = context });

        Assert.True(await checker.HasAsync(userId, "users.write", CancellationToken.None));
        Assert.True(await checker.HasAnyAsync(userId, ["users.delete"], CancellationToken.None));
        Assert.True(await checker.HasAllAsync(userId, ["users.delete", "users.write"], CancellationToken.None));

        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtClaimNames.Permission, "users.read")
        ], "test"));

        Assert.True(await checker.HasAsync(userId, "users.write", CancellationToken.None));
        Assert.False(await checker.HasAsync(Guid.NewGuid(), "users.write", CancellationToken.None));
    }

    [Fact]
    public void ClaimsExtensions_WhenRegistered_ExposePermissionCheckerThroughContract()
    {
        ServiceCollection services = new();

        services.AddPermissionCheckingFromClaims();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        ClaimsPermissionChecker implementation = scope.ServiceProvider.GetRequiredService<ClaimsPermissionChecker>();
        IPermissionChecker contract = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();

        Assert.Same(implementation, contract);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
    }

    [Fact]
    public void InternalServiceJwtIssuer_WhenIssuingToken_UsesConfiguredKeyRingAndSortedDistinctPermissions()
    {
        DateTimeOffset now = new(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        InternalServiceJwtOptions options = new()
        {
            Issuer = "https://gateway.test",
            Audience = "internal-services",
            SigningKey = "unused",
            LifetimeSeconds = 600,
            CurrentKeyId = "current",
            Keys = new Dictionary<string, string>
            {
                ["current"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&"
            }
        };

        InternalServiceJwtIssuer issuer = new(
            Options.Create(options),
            new FixedTimeProvider(now));

        string token = issuer.Issue(
            subjectId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            serviceName: "population",
            permissions: ["users.write", "users.read", "users.read"]);

        JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("current", parsed.Header.Kid);
        Assert.Equal(options.Issuer, parsed.Issuer);
        Assert.Equal(options.Audience, parsed.Audiences.Single());
        Assert.Equal("population", parsed.Claims.Single(x => x.Type == JwtClaimNames.Service).Value);
        Assert.Equal(InternalJwtTokenKinds.Service, parsed.Claims.Single(x => x.Type == JwtClaimNames.InternalTokenKind).Value);
        Assert.Equal(["users.read", "users.write"], parsed.Claims.Where(x => x.Type == JwtClaimNames.Permission).Select(x => x.Value).ToArray());
        Assert.Equal(now.UtcDateTime, parsed.IssuedAt);
        Assert.Equal(now.AddSeconds(options.LifetimeSeconds).UtcDateTime, parsed.ValidTo);
    }

    [Fact]
    public async Task InternalScopedServiceAuthenticationHandler_WhenSendingRequest_IssuesBearerTokenWithDistinctPermissions()
    {
        Guid subjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        RecordingHttpMessageHandler innerHandler = new();
        TestInternalServiceJwtIssuer issuer = new();
        InternalScopedServiceAuthenticationHandler handler = new(
            jwtIssuer: issuer,
            subjectId: subjectId,
            serviceName: "economy",
            permissions: ["budget.read", "budget.read", "", "budget.write"])
        {
            InnerHandler = innerHandler
        };

        using HttpMessageInvoker invoker = new(handler);
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.test/api");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Equal(subjectId, issuer.LastSubjectId);
        Assert.Equal("economy", issuer.LastServiceName);
        Assert.Equal(["budget.read", "budget.write"], issuer.LastPermissions);
        Assert.Equal("Bearer", innerHandler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("issued-token", innerHandler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public void InternalServicePrincipals_ExposeStableKnownIdentities()
    {
        Assert.Equal("resources", InternalServicePrincipals.Resources.ServiceName);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), InternalServicePrincipals.Resources.SubjectId);
        Assert.Equal("simulationcore", InternalServicePrincipals.SimulationCore.ServiceName);
    }

    private sealed class TestInternalServiceJwtIssuer : IInternalServiceJwtIssuer
    {
        public Guid LastSubjectId { get; private set; }
        public string? LastServiceName { get; private set; }
        public IReadOnlyCollection<string>? LastPermissions { get; private set; }

        public string Issue(Guid subjectId, string serviceName, IReadOnlyCollection<string> permissions)
        {
            LastSubjectId = subjectId;
            LastServiceName = serviceName;
            LastPermissions = permissions.ToArray();
            return "issued-token";
        }
    }
}
