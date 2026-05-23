using System.Net;
using System.Security.Claims;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Identity.Api.Authorization.Internal;
using Matrix.Identity.Api.Configurations;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Api.Tests.TestSupport;

public static class IdentityApiTestSupport
{
    private const string DefaultInternalApiKey = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&";

    public static IOptions<IdentityInternalOptions> CreateInternalOptions(
        string? apiKey = null,
        string? currentKeyId = null,
        IDictionary<string, string>? keys = null)
    {
        return Options.Create(new IdentityInternalOptions
        {
            ApiKey = apiKey ?? DefaultInternalApiKey,
            CurrentKeyId = currentKeyId,
            Keys = keys
        });
    }

    public static DefaultHttpContext CreateHttpContext(
        string path = "/",
        string? remoteIp = "198.51.100.10",
        string? forwardedClientIp = null,
        bool trustedGateway = false,
        string? userAgent = "IdentityApiTests/1.0",
        Guid? userId = null,
        Guid? sessionId = null)
    {
        DefaultHttpContext context = new();
        context.Request.Path = path;

        if (!string.IsNullOrWhiteSpace(remoteIp))
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        if (!string.IsNullOrWhiteSpace(forwardedClientIp))
            context.Request.Headers[TrustedGatewayClientHeaders.ClientIpHeaderName] = forwardedClientIp;

        if (!string.IsNullOrWhiteSpace(userAgent))
            context.Request.Headers.UserAgent = userAgent;

        if (trustedGateway)
            TrustedGatewayRequestContext.Mark(context);

        if (userId.HasValue || sessionId.HasValue)
        {
            List<Claim> claims = [];

            if (userId.HasValue)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

            if (sessionId.HasValue)
                claims.Add(new Claim(JwtClaimNames.SessionId, sessionId.Value.ToString()));

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "tests"));
        }

        return context;
    }

    public static T AttachHttpContext<T>(
        T controller,
        HttpContext httpContext)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    public static LoginUserResult CreateLoginUserResult(
        string accessToken = "access-token",
        string refreshToken = "refresh-token",
        bool isPersistent = true)
    {
        return new LoginUserResult
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            AccessTokenExpiresInSeconds = 900,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            IsPersistent = isPersistent
        };
    }

    public static MySessionResult CreateMySessionResult(
        Guid sessionId,
        bool isActive = true,
        bool isPersistent = true,
        string? ipAddress = "198.51.100.42")
    {
        return new MySessionResult
        {
            Id = sessionId,
            DeviceId = "device-1",
            DeviceName = "Desktop",
            UserAgent = "Browser/1.0",
            IpAddress = ipAddress,
            Country = "US",
            Region = "CA",
            City = "San Francisco",
            CreatedAtUtc = new DateTime(2048, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            LastUsedAtUtc = new DateTime(2048, 6, 1, 9, 0, 0, DateTimeKind.Utc),
            RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 15, 8, 0, 0, DateTimeKind.Utc),
            IsActive = isActive,
            IsPersistent = isPersistent
        };
    }

    public static AuthorizationContext CreateAuthorizationContext(
        int permissionsVersion = 3,
        params string[] permissions)
    {
        return new AuthorizationContext(
            Roles: ["user"],
            Permissions: permissions,
            PermissionsVersion: permissionsVersion);
    }

    public sealed class FakeSender : ISender
    {
        private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();

        public List<object> Requests { get; } = [];

        public void Handle<TRequest, TResponse>(Func<TRequest, TResponse> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = (request, _) => Task.FromResult<object?>(handler((TRequest)request));
        }

        public void HandleAsync<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = async (request, cancellationToken) => await handler((TRequest)request, cancellationToken);
        }

        public void Handle<TRequest>(Action<TRequest> handler)
            where TRequest : notnull
        {
            _handlers[typeof(TRequest)] = (request, _) =>
            {
                handler((TRequest)request);
                return Task.FromResult<object?>(Unit.Value);
            };
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            return InvokeTyped<TResponse>(handler, request, cancellationToken);
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            await handler(request, cancellationToken);
        }

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
                throw new InvalidOperationException($"No handler registered for request type '{request.GetType().Name}'.");

            return await handler(request, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        private static async Task<TResponse> InvokeTyped<TResponse>(
            Func<object, CancellationToken, Task<object?>> handler,
            object request,
            CancellationToken cancellationToken)
        {
            object? result = await handler(request, cancellationToken);
            return (TResponse)result!;
        }
    }

    public sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, int?> PermissionsVersions { get; } = new();

        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            PermissionsVersions.TryGetValue(userId, out int? version);
            return Task.FromResult(version);
        }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public sealed class FakeEffectivePermissionsService : IEffectivePermissionsService
    {
        public AuthorizationContext? Result { get; set; }
        public Exception? Exception { get; set; }
        public Guid? LastRequestedUserId { get; private set; }

        public Task<AuthorizationContext> GetAuthContextAsync(Guid userId, CancellationToken cancellationToken)
        {
            LastRequestedUserId = userId;

            if (Exception is not null)
                throw Exception;

            return Task.FromResult(Result ?? CreateAuthorizationContext());
        }
    }

    public sealed class FakeDefaultUserAccessPolicyRepository : IDefaultUserAccessPolicyRepository
    {
        public int Version { get; set; } = 1;

        public Task<int> GetVersionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Version);
        }

        public Task<DefaultUserAccessPolicy> GetForUpdateAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, PermissionEffect>> GetOverridesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ReplaceOverridesAsync(IReadOnlyDictionary<string, PermissionEffect> overrides, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public sealed class FakeAvatarStorage : IAvatarStorage
    {
        public string? LastOpenedPath { get; private set; }
        public AvatarFileReadResult? OpenReadResult { get; set; }

        public Task<AvatarFileReadResult?> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        {
            LastOpenedPath = path;
            return Task.FromResult(OpenReadResult);
        }

        public Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
