using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Authorization.PermissionsVersion
{
    public sealed class ExternalJwtPermissionsVersionEventsTests
    {
        [Fact]
        public async Task HandleTokenValidated_WhenTokenVersionIsStale_MarksRequestAndFails()
        {
            var userId = Guid.Parse("54d339bc-5efb-4591-b895-82a92c1bf174");
            var store = new FakePermissionsVersionStore
            {
                CurrentVersion = 9
            };
            TokenValidatedContext context = CreateTokenValidatedContext(
                principal: CreatePrincipal(
                    userId: userId,
                    permissionsVersion: "4"),
                services: CreateServiceProvider(store));

            await ExternalJwtPermissionsVersionEvents.HandleTokenValidated(context);

            Assert.Equal(
                expected: userId,
                actual: store.LastRequestedUserId);
            Assert.True(context.HttpContext.Items.ContainsKey(PermissionsVersionValidationDefaults.StaleTokenItemKey));
            Assert.NotNull(context.Result);
            Assert.NotNull(context.Result.Failure);
            Assert.Equal(
                expected: "token_stale",
                actual: context.Result.Failure!.Message);
        }

        [Fact]
        public async Task HandleTokenValidated_WhenDependencyIsUnavailable_MarksUnavailableAndFails()
        {
            var userId = Guid.Parse("3e97a618-39cc-45d9-b412-8cf5268c1111");
            var store = new FakePermissionsVersionStore
            {
                Exception = new PermissionsVersionUnavailableException(
                    userId: userId,
                    innerException: new HttpRequestException("identity unavailable"))
            };
            TokenValidatedContext context = CreateTokenValidatedContext(
                principal: CreatePrincipal(
                    userId: userId,
                    permissionsVersion: "8"),
                services: CreateServiceProvider(store));

            await ExternalJwtPermissionsVersionEvents.HandleTokenValidated(context);

            Assert.True(context.HttpContext.Items.ContainsKey(PermissionsVersionValidationDefaults.UnavailableItemKey));
            Assert.NotNull(context.Result);
            Assert.NotNull(context.Result.Failure);
            Assert.Equal(
                expected: "permissions_version_unavailable",
                actual: context.Result.Failure!.Message);
        }

        [Fact]
        public async Task HandleTokenValidated_WhenUserIdClaimIsInvalid_FailsWithoutCallingStore()
        {
            var store = new FakePermissionsVersionStore
            {
                CurrentVersion = 2
            };
            TokenValidatedContext context = CreateTokenValidatedContext(
                principal: new ClaimsPrincipal(
                    new ClaimsIdentity(
                        claims:
                        [
                            new Claim(
                                type: JwtRegisteredClaimNames.Sub,
                                value: "not-a-guid"),
                            new Claim(
                                type: JwtClaimNames.PermissionsVersion,
                                value: "5")
                        ],
                        authenticationType: "Bearer")),
                services: CreateServiceProvider(store));

            await ExternalJwtPermissionsVersionEvents.HandleTokenValidated(context);

            Assert.Equal(
                expected: 0,
                actual: store.GetCurrentCallCount);
            Assert.NotNull(context.Result);
            Assert.NotNull(context.Result.Failure);
            Assert.Equal(
                expected: "invalid_token",
                actual: context.Result.Failure!.Message);
        }

        [Fact]
        public async Task HandleChallenge_WhenUnavailableFlagIsSet_Writes503ProblemDetails()
        {
            JwtBearerChallengeContext context = CreateChallengeContext();
            context.HttpContext.Items[PermissionsVersionValidationDefaults.UnavailableItemKey] = true;

            await ExternalJwtPermissionsVersionEvents.HandleChallenge(context);

            JsonElement problem = await ReadProblemAsync(context.HttpContext);

            Assert.Equal(
                expected: StatusCodes.Status503ServiceUnavailable,
                actual: context.HttpContext.Response.StatusCode);
            Assert.Equal(
                expected: "application/problem+json",
                actual: context.HttpContext.Response.ContentType);
            Assert.Equal(
                expected: "Auth.DependencyUnavailable",
                actual: problem.GetProperty("code")
                   .GetString());
            Assert.Equal(
                expected: "Authentication dependency is temporarily unavailable. Please retry.",
                actual: problem.GetProperty("message")
                   .GetString());
        }

        [Fact]
        public async Task HandleChallenge_WhenStaleTokenFlagIsSet_Writes401ProblemDetails()
        {
            JwtBearerChallengeContext context = CreateChallengeContext();
            context.HttpContext.Items[PermissionsVersionValidationDefaults.StaleTokenItemKey] = true;

            await ExternalJwtPermissionsVersionEvents.HandleChallenge(context);

            JsonElement problem = await ReadProblemAsync(context.HttpContext);

            Assert.Equal(
                expected: StatusCodes.Status401Unauthorized,
                actual: context.HttpContext.Response.StatusCode);
            Assert.Equal(
                expected: "application/problem+json",
                actual: context.HttpContext.Response.ContentType);
            Assert.Equal(
                expected: "Auth.TokenStale",
                actual: problem.GetProperty("code")
                   .GetString());
            Assert.Equal(
                expected: "Access token is stale. Refresh required.",
                actual: problem.GetProperty("message")
                   .GetString());
        }

        private static TokenValidatedContext CreateTokenValidatedContext(
            ClaimsPrincipal principal,
            IServiceProvider services)
        {
            DefaultHttpContext httpContext = CreateHttpContext(services);
            var context = new TokenValidatedContext(
                context: httpContext,
                scheme: CreateScheme(),
                options: new JwtBearerOptions())
            {
                Principal = principal
            };

            return context;
        }

        private static JwtBearerChallengeContext CreateChallengeContext()
        {
            DefaultHttpContext httpContext = CreateHttpContext(CreateServiceProvider());
            return new JwtBearerChallengeContext(
                context: httpContext,
                scheme: CreateScheme(),
                options: new JwtBearerOptions(),
                properties: new AuthenticationProperties());
        }

        private static DefaultHttpContext CreateHttpContext(IServiceProvider services)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = services
            };
            httpContext.TraceIdentifier = "gateway-trace";
            httpContext.Request.Path = "/gateway/test";
            httpContext.Response.Body = new MemoryStream();
            return httpContext;
        }

        private static AuthenticationScheme CreateScheme()
        {
            return new AuthenticationScheme(
                name: JwtBearerDefaults.AuthenticationScheme,
                displayName: JwtBearerDefaults.AuthenticationScheme,
                handlerType: typeof(JwtBearerHandler));
        }

        private static ClaimsPrincipal CreatePrincipal(
            Guid userId,
            string permissionsVersion)
        {
            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtRegisteredClaimNames.Sub,
                            value: userId.ToString()),
                        new Claim(
                            type: JwtClaimNames.PermissionsVersion,
                            value: permissionsVersion)
                    ],
                    authenticationType: "Bearer"));
        }

        private static async Task<JsonElement> ReadProblemAsync(HttpContext context)
        {
            context.Response.Body.Position = 0;
            using JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            return document.RootElement.Clone();
        }
    }
}
