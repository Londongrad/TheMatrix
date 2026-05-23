using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users;

internal static class AdminUsersTestSupport
{
    internal static readonly DateTime UtcNow = new(2047, 7, 2, 8, 9, 10, DateTimeKind.Utc);
    internal static readonly DateTime RefreshTokenExpiresAtUtc = UtcNow.AddDays(7);

    internal static TimeProvider CreateTimeProvider(DateTime? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? UtcNow);
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
            createdAtUtc: UtcNow.AddDays(-10));

        if (isLocked)
            user.Lock();

        if (isDeleted)
            user.SoftDelete(UtcNow.AddDays(-1));

        return user;
    }

    internal static UserSession CreateSession(
        User user,
        string deviceId = "device-1",
        DateTime? expiresAtUtc = null,
        bool isRevoked = false)
    {
        var session = UserSession.Create(
            userId: user.Id,
            deviceInfo: DeviceInfo.Create(deviceId, "Phone", "Mozilla/5.0", "127.0.0.1"),
            geoLocation: GeoLocation.Create("Russia", "Zabaykalsky Krai", "Chita"),
            refreshTokenExpiresAtUtc: expiresAtUtc ?? RefreshTokenExpiresAtUtc,
            isPersistent: true,
            createdAtUtc: UtcNow.AddHours(-2));

        if (isRevoked)
            session.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: UtcNow.AddMinutes(-15));

        return session;
    }

    internal static RefreshToken SeedRefreshToken(
        User user,
        Guid sessionId,
        string tokenHash = "refresh-token-hash",
        bool isRevoked = false)
    {
        RefreshToken token = user.IssueRefreshToken(
            sessionId: sessionId,
            tokenHash: tokenHash,
            expiresAtUtc: RefreshTokenExpiresAtUtc,
            deviceInfo: DeviceInfo.Create("device-1", "Phone", "Mozilla/5.0", "127.0.0.1"),
            geoLocation: GeoLocation.Create("Russia", "Zabaykalsky Krai", "Chita"),
            isPersistent: true,
            createdAtUtc: UtcNow.AddHours(-2));

        if (isRevoked)
            token.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: UtcNow.AddMinutes(-15));

        return token;
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public User? UserById { get; set; }
        public User? UserByIdWithRefreshTokens { get; set; }
        public bool ExistsAsyncResult { get; set; }
        public Guid? RequestedUserId { get; private set; }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            return Task.FromResult(UserById);
        }

        public Task<User?> GetByIdWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            return Task.FromResult(UserByIdWithRefreshTokens);
        }

        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            return Task.FromResult(ExistsAsyncResult);
        }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeUserAdminReadRepository : IUserAdminReadRepository
    {
        public Pagination? RequestedPagination { get; private set; }
        public PagedResult<UserListItemResult> Result { get; set; } = new(
            items: Array.Empty<UserListItemResult>(),
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10);

        public Task<PagedResult<UserListItemResult>> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken)
        {
            RequestedPagination = pagination;
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public List<UserSession> Sessions { get; } = new();
        public Guid? RequestedUserId { get; private set; }

        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            IReadOnlyCollection<UserSession> items = Sessions.Where(x => x.UserId == userId).ToArray();
            return Task.FromResult(items);
        }

        public Task<DateTime?> GetLastVisitedAtUtcAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            DateTime? lastVisited = Sessions
                .Where(x => x.UserId == userId)
                .Select(x => x.LastUsedAtUtc ?? x.CreatedAtUtc)
                .OrderByDescending(x => x)
                .FirstOrDefault();
            return Task.FromResult(lastVisited);
        }

        public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserSession?> GetActiveByUserIdAndDeviceIdAsync(Guid userId, string deviceId, DateTime utcNow, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSession>> ListActiveByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<UserSession> Items, int TotalCount)> GetEndedPageByUserIdAsync(Guid userId, DateTime utcNow, Pagination pagination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeUserRolesRepository : IUserRolesRepository
    {
        public IReadOnlyCollection<UserRoleResult> GetUserRolesResult { get; set; } = Array.Empty<UserRoleResult>();
        public Guid? RequestedUserId { get; private set; }
        public IReadOnlyCollection<Guid>? ReplacedRoleIds { get; private set; }
        public bool ReplaceResult { get; set; }

        public Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            return Task.FromResult(GetUserRolesResult);
        }

        public Task<bool> ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            ReplacedRoleIds = roleIds.ToArray();
            return Task.FromResult(ReplaceResult);
        }
    }

    internal sealed class FakeUserPermissionsRepository : IUserPermissionsRepository
    {
        public IReadOnlyCollection<UserPermissionOverrideResult> GetUserPermissionsResult { get; set; } = Array.Empty<UserPermissionOverrideResult>();
        public Guid? RequestedUserId { get; private set; }
        public string? RequestedPermissionKey { get; private set; }
        public PermissionEffect? RequestedEffect { get; private set; }
        public IReadOnlyDictionary<string, PermissionEffect>? ReplacedPermissionEffects { get; private set; }
        public bool UpsertResult { get; set; }
        public bool ReplaceResult { get; set; }

        public Task<IReadOnlyCollection<UserPermissionOverrideResult>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            return Task.FromResult(GetUserPermissionsResult);
        }

        public Task<bool> UpsertUserPermissionAsync(Guid userId, string permissionKey, PermissionEffect effect, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            RequestedPermissionKey = permissionKey;
            RequestedEffect = effect;
            return Task.FromResult(UpsertResult);
        }

        public Task<bool> ReplaceUserPermissionsAsync(Guid userId, IReadOnlyDictionary<string, PermissionEffect> permissionEffects, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            ReplacedPermissionEffects = new Dictionary<string, PermissionEffect>(permissionEffects, StringComparer.Ordinal);
            return Task.FromResult(ReplaceResult);
        }
    }

    internal sealed class FakePermissionReadRepository : IPermissionReadRepository
    {
        public IReadOnlyCollection<PermissionCatalogItemResult> Permissions { get; set; } = Array.Empty<PermissionCatalogItemResult>();
        public Dictionary<string, PermissionCatalogItemResult> PermissionByKey { get; } = new(StringComparer.Ordinal);
        public string? RequestedPermissionKey { get; private set; }

        public Task<IReadOnlyCollection<PermissionCatalogItemResult>> GetPermissionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Permissions);
        }

        public Task<PermissionCatalogItemResult?> GetPermissionAsync(string permissionKey, CancellationToken cancellationToken)
        {
            RequestedPermissionKey = permissionKey;
            PermissionByKey.TryGetValue(permissionKey, out PermissionCatalogItemResult? permission);
            return Task.FromResult(permission);
        }
    }

    internal sealed class FakeAdminUserGuard : IAdminUserGuard
    {
        public Guid? RequestedTargetUserId { get; private set; }
        public IReadOnlyCollection<Guid>? RequestedDesiredRoleIds { get; private set; }
        public Exception? ManageException { get; set; }
        public Exception? RoleAssignmentException { get; set; }

        public Task EnsureUserCanBeManagedAsync(Guid targetUserId, CancellationToken cancellationToken)
        {
            RequestedTargetUserId = targetUserId;
            if (ManageException is not null)
                throw ManageException;
            return Task.CompletedTask;
        }

        public Task EnsureRoleAssignmentIsAllowedAsync(IReadOnlyCollection<Guid> desiredRoleIds, CancellationToken cancellationToken)
        {
            RequestedDesiredRoleIds = desiredRoleIds.ToArray();
            if (RoleAssignmentException is not null)
                throw RoleAssignmentException;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeRoleIdsValidator : Matrix.Identity.Application.Abstractions.Services.Validation.IRoleIdsValidator
    {
        public IReadOnlyCollection<Guid>? ValidatedRoleIds { get; private set; }
        public Exception? ValidateException { get; set; }

        public Task ValidateExistAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
        {
            ValidatedRoleIds = roleIds.ToArray();
            if (ValidateException is not null)
                throw ValidateException;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeDefaultUserAccessPolicyRepository : IDefaultUserAccessPolicyRepository
    {
        public int VersionResult { get; set; }
        public IReadOnlyDictionary<string, PermissionEffect> OverridesResult { get; set; } = new Dictionary<string, PermissionEffect>(StringComparer.Ordinal);
        public bool ReplaceResult { get; set; }
        public DefaultUserAccessPolicy PolicyForUpdate { get; set; } = DefaultUserAccessPolicy.CreateDefault(UtcNow.AddDays(-1));
        public IReadOnlyDictionary<string, PermissionEffect>? ReplacedOverrides { get; private set; }

        public Task<DefaultUserAccessPolicy> GetForUpdateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(PolicyForUpdate);
        }

        public Task<int> GetVersionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(VersionResult);
        }

        public Task<IReadOnlyDictionary<string, PermissionEffect>> GetOverridesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(OverridesResult);
        }

        public Task<bool> ReplaceOverridesAsync(IReadOnlyDictionary<string, PermissionEffect> overrides, CancellationToken cancellationToken)
        {
            ReplacedOverrides = new Dictionary<string, PermissionEffect>(overrides, StringComparer.Ordinal);
            return Task.FromResult(ReplaceResult);
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

    internal sealed class FakeSecurityAuditService : ISecurityAuditService
    {
        public List<SecurityAuditEntry> Entries { get; } = new();

        public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> IsLoginAllowedAsync(string loginSubject, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailConfirmationRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailChangeRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsPasswordResetRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsAccountRecoveryRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeEmailSender : IEmailSender
    {
        public List<string> AccountRestoredEmails { get; } = new();
        public Exception? SendAccountRestoredException { get; set; }

        public Task SendAccountRestored(string toEmail, CancellationToken cancellationToken)
        {
            AccountRestoredEmails.Add(toEmail);
            if (SendAccountRestoredException is not null)
                throw SendAccountRestoredException;
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmation(string toEmail, string confirmationLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendPasswordReset(string toEmail, string resetLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendUsernameChanged(string toEmail, string previousUsername, string newUsername, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendEmailChangeConfirmation(string toEmail, string currentEmail, string confirmationLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendAccountDeleted(string toEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendAccountRecovery(string toEmail, string recoveryLink, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeCurrentUserContext : Matrix.BuildingBlocks.Application.Abstractions.ICurrentUserContext
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid? UserId { get; set; }
        public Guid? SessionId { get; set; }
    }
}
