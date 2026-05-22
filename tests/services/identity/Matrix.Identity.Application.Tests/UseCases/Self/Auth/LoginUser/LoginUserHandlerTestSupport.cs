using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Self.Auth;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using ProdLoginUser = Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser;

internal static class LoginUserHandlerTestSupport
{
    internal static readonly DateTime UtcNow = new(2047, 6, 8, 9, 10, 11, DateTimeKind.Utc);
    internal static readonly DateTime RefreshTokenExpiresAtUtc = UtcNow.AddDays(7);

    internal static TimeProvider CreateTimeProvider(DateTime? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? UtcNow);
    }

    internal static ProdLoginUser.LoginUserCommand CreateCommand(
        string login = "neo@matrix.local",
        string password = "Pa$$w0rd",
        string deviceId = "device-1",
        string deviceName = "Phone",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1",
        bool rememberMe = true)
    {
        return new ProdLoginUser.LoginUserCommand(
            Login: login,
            Password: password,
            DeviceId: deviceId,
            DeviceName: deviceName,
            UserAgent: userAgent,
            IpAddress: ipAddress,
            RememberMe: rememberMe);
    }

    internal static User CreateUser(
        string email = "neo@matrix.local",
        string username = "neo",
        string passwordHash = "stored-hash",
        bool isLocked = false,
        bool isDeleted = false)
    {
        var user = User.CreateNew(
            email: Email.Create(email),
            username: Username.Create(username),
            passwordHash: passwordHash,
            createdAtUtc: UtcNow.AddDays(-1));

        if (isLocked)
            user.Lock();

        if (isDeleted)
            user.SoftDelete(UtcNow.AddHours(-1));

        return user;
    }

    internal static DeviceInfo CreateDeviceInfo(
        string deviceId = "device-1",
        string deviceName = "Phone",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1")
    {
        return DeviceInfo.Create(
            deviceId: deviceId,
            deviceName: deviceName,
            userAgent: userAgent,
            ipAddress: ipAddress);
    }

    internal static GeoLocation CreateGeoLocation()
    {
        return GeoLocation.Create(
            country: "Russia",
            region: "Zabaykalsky Krai",
            city: "Chita");
    }

    internal static UserSession CreateSession(
        User user,
        string deviceId = "device-1",
        string deviceName = "Phone",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1",
        DateTime? expiresAtUtc = null,
        bool isPersistent = true)
    {
        return UserSession.Create(
            userId: user.Id,
            deviceInfo: CreateDeviceInfo(deviceId, deviceName, userAgent, ipAddress),
            geoLocation: CreateGeoLocation(),
            refreshTokenExpiresAtUtc: expiresAtUtc ?? RefreshTokenExpiresAtUtc,
            isPersistent: isPersistent,
            createdAtUtc: UtcNow.AddMinutes(-30));
    }

    internal static void SeedRefreshToken(
        User user,
        Guid sessionId,
        string tokenHash,
        string deviceId = "device-1",
        string deviceName = "Phone",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1",
        DateTime? expiresAtUtc = null,
        bool isPersistent = true)
    {
        user.IssueRefreshToken(
            sessionId: sessionId,
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc ?? RefreshTokenExpiresAtUtc,
            deviceInfo: CreateDeviceInfo(deviceId, deviceName, userAgent, ipAddress),
            geoLocation: CreateGeoLocation(),
            isPersistent: isPersistent,
            createdAtUtc: UtcNow.AddMinutes(-30));
    }

    internal static ProdLoginUser.LoginUserCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakePasswordHasher passwordHasher,
        FakeAccessTokenService accessTokenService,
        FakeRefreshTokenProvider refreshTokenProvider,
        FakeGeoLocationService geoLocationService,
        FakeUnitOfWork unitOfWork,
        FakeEffectivePermissionsService permissionsService,
        FakeSecurityAuditService securityAuditService,
        TimeProvider? timeProvider = null)
    {
        return new ProdLoginUser.LoginUserCommandHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            permissionsService,
            timeProvider ?? CreateTimeProvider(),
            securityAuditService);
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public User? UserByEmail { get; set; }
        public User? UserByUsername { get; set; }
        public string? RequestedEmail { get; private set; }
        public string? RequestedUsername { get; private set; }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            RequestedEmail = normalizedEmail;
            return Task.FromResult(UserByEmail);
        }

        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default)
        {
            RequestedUsername = login;
            return Task.FromResult(UserByUsername);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public List<UserSession> Sessions { get; } = new();
        public List<UserSession> AddedSessions { get; } = new();

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            AddedSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<UserSession?> GetActiveByUserIdAndDeviceIdAsync(Guid userId, string deviceId, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            UserSession? session = Sessions.FirstOrDefault(x =>
                x.UserId == userId &&
                x.DeviceInfo.DeviceId == deviceId &&
                x.IsActive(utcNow));
            return Task.FromResult(session);
        }

        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<UserSession> items = Sessions
               .Where(x => x.UserId == userId && x.DeviceInfo.DeviceId == deviceId)
               .ToArray();
            return Task.FromResult(items);
        }

        public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<UserSession> Items, int TotalCount)> GetEndedPageByUserIdAsync(Guid userId, DateTime utcNow, Pagination pagination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DateTime?> GetLastVisitedAtUtcAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSession>> ListActiveByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordVerificationOutcome VerifyOutcome { get; set; } = PasswordVerificationOutcome.Success;
        public List<string> HashedPasswords { get; } = new();

        public string Hash(string password)
        {
            HashedPasswords.Add(password);
            return $"hash::{password}";
        }

        public PasswordVerificationOutcome Verify(User user, string passwordHash, string providedPassword)
        {
            return VerifyOutcome;
        }
    }

    internal sealed class FakeAccessTokenService : IAccessTokenService
    {
        public Guid? RequestedUserId { get; private set; }
        public int? RequestedPermissionsVersion { get; private set; }
        public Guid? RequestedSessionId { get; private set; }
        public AccessTokenModel Result { get; set; } = new()
        {
            Token = "access-token",
            TokenType = "Bearer",
            ExpiresInSeconds = 900
        };

        public AccessTokenModel Generate(Guid userId, int permissionsVersion, Guid sessionId)
        {
            RequestedUserId = userId;
            RequestedPermissionsVersion = permissionsVersion;
            RequestedSessionId = sessionId;
            return Result;
        }
    }

    internal sealed class FakeRefreshTokenProvider : IRefreshTokenProvider
    {
        public bool? RequestedRememberMe { get; private set; }
        public RefreshTokenDescriptor Result { get; set; } = new(
            Token: "refresh-token",
            TokenHash: "refresh-token-hash",
            ExpiresAtUtc: RefreshTokenExpiresAtUtc);

        public string ComputeHash(string token) => throw new NotSupportedException();

        public RefreshTokenDescriptor Generate(bool isPersistent)
        {
            RequestedRememberMe = isPersistent;
            return Result;
        }
    }

    internal sealed class FakeGeoLocationService : IGeoLocationService
    {
        public List<string> RequestedIpAddresses { get; } = new();
        public GeoLocation? Result { get; set; }

        public Task<GeoLocation?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            RequestedIpAddresses.Add(ipAddress);
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeEffectivePermissionsService : IEffectivePermissionsService
    {
        public List<Guid> RequestedUserIds { get; } = new();
        public AuthorizationContext Result { get; set; } = new(
            Roles: Array.Empty<string>(),
            Permissions: Array.Empty<string>(),
            PermissionsVersion: 7);

        public Task<AuthorizationContext> GetAuthContextAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserIds.Add(userId);
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeSecurityAuditService : ISecurityAuditService
    {
        public bool IsLoginAllowedResult { get; set; } = true;
        public List<(string LoginSubject, string? IpAddress)> LoginAllowedChecks { get; } = new();
        public List<SecurityAuditEntry> Entries { get; } = new();

        public Task<bool> IsLoginAllowedAsync(string loginSubject, string? ipAddress, CancellationToken cancellationToken)
        {
            LoginAllowedChecks.Add((loginSubject, ipAddress));
            return Task.FromResult(IsLoginAllowedResult);
        }

        public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> IsAccountRecoveryRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailChangeRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailConfirmationRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsPasswordResetRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public int TransactionCalls { get; private set; }
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCalls++;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCalls++;
            LastIsolationLevel = isolationLevel;
            return action(cancellationToken);
        }
    }

    private sealed class FrozenTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(
                DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
                TimeSpan.Zero);
        }
    }
}
