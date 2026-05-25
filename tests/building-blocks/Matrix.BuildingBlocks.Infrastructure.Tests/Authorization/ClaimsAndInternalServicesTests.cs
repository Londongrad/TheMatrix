using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Infrastructure.Authorization.Claims;
using Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices;
using Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.Authorization
{
    public sealed class ClaimsAndInternalServicesTests
    {
        [Fact]
        public async Task ClaimsPermissionChecker_WhenWildcardExists_AllowsChecksAndCachesByUser()
        {
            var userId = Guid.NewGuid();
            DefaultHttpContext context = new();
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtClaimNames.Permission,
                            value: "*"),
                        new Claim(
                            type: JwtClaimNames.Permission,
                            value: "users.read")
                    ],
                    authenticationType: "test"));

            ClaimsPermissionChecker checker = new(
                new HttpContextAccessor
                {
                    HttpContext = context
                });

            Assert.True(
                await checker.HasAsync(
                    userId: userId,
                    permissionKey: "users.write",
                    cancellationToken: CancellationToken.None));
            Assert.True(
                await checker.HasAnyAsync(
                    userId: userId,
                    permissionKeys: ["users.delete"],
                    cancellationToken: CancellationToken.None));
            Assert.True(
                await checker.HasAllAsync(
                    userId: userId,
                    permissionKeys:
                    [
                        "users.delete",
                        "users.write"
                    ],
                    cancellationToken: CancellationToken.None));

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtClaimNames.Permission,
                            value: "users.read")
                    ],
                    authenticationType: "test"));

            Assert.True(
                await checker.HasAsync(
                    userId: userId,
                    permissionKey: "users.write",
                    cancellationToken: CancellationToken.None));
            Assert.False(
                await checker.HasAsync(
                    userId: Guid.NewGuid(),
                    permissionKey: "users.write",
                    cancellationToken: CancellationToken.None));
        }

        [Fact]
        public void ClaimsExtensions_WhenRegistered_ExposePermissionCheckerThroughContract()
        {
            ServiceCollection services = new();

            services.AddPermissionCheckingFromClaims();

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            ClaimsPermissionChecker implementation =
                scope.ServiceProvider.GetRequiredService<ClaimsPermissionChecker>();
            IPermissionChecker contract = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();

            Assert.Same(
                expected: implementation,
                actual: contract);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>());
        }

        [Fact]
        public void InternalServiceJwtIssuer_WhenIssuingToken_UsesConfiguredKeyRingAndSortedDistinctPermissions()
        {
            DateTimeOffset now = new(
                year: 2026,
                month: 5,
                day: 19,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
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
                options: Options.Create(options),
                timeProvider: new FixedTimeProvider(now));

            string token = issuer.Issue(
                subjectId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                serviceName: "population",
                permissions:
                [
                    "users.write",
                    "users.read",
                    "users.read"
                ]);

            JwtSecurityToken parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(
                expected: "current",
                actual: parsed.Header.Kid);
            Assert.Equal(
                expected: options.Issuer,
                actual: parsed.Issuer);
            Assert.Equal(
                expected: options.Audience,
                actual: parsed.Audiences.Single());
            Assert.Equal(
                expected: "population",
                actual: parsed.Claims.Single(x => x.Type == JwtClaimNames.Service)
                   .Value);
            Assert.Equal(
                expected: InternalJwtTokenKinds.Service,
                actual: parsed.Claims.Single(x => x.Type == JwtClaimNames.InternalTokenKind)
                   .Value);
            Assert.Equal(
                expectedSpan:
                [
                    "users.read",
                    "users.write"
                ],
                actualArray: parsed.Claims.Where(x => x.Type == JwtClaimNames.Permission)
                   .Select(x => x.Value)
                   .ToArray());
            Assert.Equal(
                expected: now.UtcDateTime,
                actual: parsed.IssuedAt);
            Assert.Equal(
                expected: now.AddSeconds(options.LifetimeSeconds)
                   .UtcDateTime,
                actual: parsed.ValidTo);
        }

        [Fact]
        public async Task
            InternalScopedServiceAuthenticationHandler_WhenSendingRequest_IssuesBearerTokenWithDistinctPermissions()
        {
            var subjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            RecordingHttpMessageHandler innerHandler = new();
            TestInternalServiceJwtIssuer issuer = new();
            InternalScopedServiceAuthenticationHandler handler = new(
                jwtIssuer: issuer,
                subjectId: subjectId,
                serviceName: "economy",
                permissions:
                [
                    "budget.read",
                    "budget.read",
                    "",
                    "budget.write"
                ])
            {
                InnerHandler = innerHandler
            };

            using HttpMessageInvoker invoker = new(handler);
            using HttpRequestMessage request = new(
                method: HttpMethod.Get,
                requestUri: "https://example.test/api");

            await invoker.SendAsync(
                request: request,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(innerHandler.LastRequest);
            Assert.Equal(
                expected: subjectId,
                actual: issuer.LastSubjectId);
            Assert.Equal(
                expected: "economy",
                actual: issuer.LastServiceName);
            Assert.Equal(
                expected:
                [
                    "budget.read",
                    "budget.write"
                ],
                actual: issuer.LastPermissions);
            Assert.Equal(
                expected: "Bearer",
                actual: innerHandler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal(
                expected: "issued-token",
                actual: innerHandler.LastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public void InternalServicePrincipals_ExposeStableKnownIdentities()
        {
            Assert.Equal(
                expected: "resources",
                actual: InternalServicePrincipals.Resources.ServiceName);
            Assert.Equal(
                expected: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                actual: InternalServicePrincipals.Resources.SubjectId);
            Assert.Equal(
                expected: "simulationcore",
                actual: InternalServicePrincipals.SimulationCore.ServiceName);
        }

        private sealed class TestInternalServiceJwtIssuer : IInternalServiceJwtIssuer
        {
            public Guid LastSubjectId { get; private set; }
            public string? LastServiceName { get; private set; }
            public IReadOnlyCollection<string>? LastPermissions { get; private set; }

            public string Issue(
                Guid subjectId,
                string serviceName,
                IReadOnlyCollection<string> permissions)
            {
                LastSubjectId = subjectId;
                LastServiceName = serviceName;
                LastPermissions = permissions.ToArray();
                return "issued-token";
            }
        }
    }
}
