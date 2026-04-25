using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.Tests.UseCases.Self;

internal static class SelfServiceHandlerTestSupport
{
    internal static readonly DateTime UtcNow = new(2047, 6, 9, 10, 11, 12, DateTimeKind.Utc);
    internal static readonly DateTime RefreshTokenExpiresAtUtc = UtcNow.AddDays(7);
    internal static readonly DateTime RotatedRefreshTokenExpiresAtUtc = UtcNow.AddDays(14);

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
        bool isPersistent = true,
        bool isRevoked = false)
    {
        var session = UserSession.Create(
            userId: user.Id,
            deviceInfo: CreateDeviceInfo(deviceId, deviceName, userAgent, ipAddress),
            geoLocation: CreateGeoLocation(),
            refreshTokenExpiresAtUtc: expiresAtUtc ?? RefreshTokenExpiresAtUtc,
            isPersistent: isPersistent,
            createdAtUtc: UtcNow.AddMinutes(-30));

        if (isRevoked)
            session.Revoke(
                reason: Domain.Enums.RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: UtcNow.AddMinutes(-1));

        return session;
    }

    internal static RefreshToken SeedRefreshToken(
        User user,
        Guid sessionId,
        string tokenHash,
        string deviceId = "device-1",
        string deviceName = "Phone",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1",
        DateTime? expiresAtUtc = null,
        bool isPersistent = true,
        bool isRevoked = false)
    {
        RefreshToken refreshToken = user.IssueRefreshToken(
            sessionId: sessionId,
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc ?? RefreshTokenExpiresAtUtc,
            deviceInfo: CreateDeviceInfo(deviceId, deviceName, userAgent, ipAddress),
            geoLocation: CreateGeoLocation(),
            isPersistent: isPersistent,
            createdAtUtc: UtcNow.AddMinutes(-30));

        if (isRevoked)
            refreshToken.Revoke(
                reason: Domain.Enums.RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: UtcNow.AddMinutes(-1));

        return refreshToken;
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken.RefreshTokenCommand CreateRefreshCommand(
        string refreshToken = "incoming-refresh-token",
        string deviceId = "device-1",
        string userAgent = "Mozilla/5.0",
        string? ipAddress = "127.0.0.1")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken.RefreshTokenCommand(
            RefreshToken: refreshToken,
            DeviceId: deviceId,
            UserAgent: userAgent,
            IpAddress: ipAddress);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken.RevokeRefreshTokenCommand CreateRevokeRefreshTokenCommand(
        string refreshToken = "incoming-refresh-token",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken.RevokeRefreshTokenCommand(
            RefreshToken: refreshToken,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession.RevokeMySessionCommand CreateRevokeMySessionCommand(Guid sessionId)
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession.RevokeMySessionCommand(sessionId);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions.RevokeOtherMySessionsCommand CreateRevokeOtherMySessionsCommand()
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions.RevokeOtherMySessionsCommand();
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions.RevokeAllMySessionsCommand CreateRevokeAllMySessionsCommand()
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions.RevokeAllMySessionsCommand();
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.ChangePassword.ChangePasswordCommand CreateChangePasswordCommand(
        string currentPassword = "CurrentPa$$w0rd",
        string newPassword = "NewPa$$w0rd")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.ChangePassword.ChangePasswordCommand(
            CurrentPassword: currentPassword,
            NewPassword: newPassword);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommand CreateDeleteMyAccountCommand(
        string currentPassword = "CurrentPa$$w0rd",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommand(
            CurrentPassword: currentPassword,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword.ResetPasswordCommand CreateResetPasswordCommand(
        Guid userId,
        string token = "raw-reset-token",
        string newPassword = "NewPa$$w0rd",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword.ResetPasswordCommand(
            UserId: userId,
            Token: token,
            NewPassword: newPassword,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset.SendPasswordResetCommand CreateSendPasswordResetCommand(
        string email = "neo@matrix.local",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset.SendPasswordResetCommand(
            Email: email,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommand CreateRequestEmailChangeCommand(
        string newEmail = "new.neo@matrix.local",
        string currentPassword = "CurrentPa$$w0rd",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange.RequestEmailChangeCommand(
            NewEmail: newEmail,
            CurrentPassword: currentPassword,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommand CreateConfirmEmailChangeCommand(
        Guid userId,
        string token = "raw-email-change-token",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange.ConfirmEmailChangeCommand(
            UserId: userId,
            Token: token,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange.CancelPendingEmailChangeCommand CreateCancelPendingEmailChangeCommand(
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange.CancelPendingEmailChangeCommand(
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange.ResendPendingEmailChangeCommand CreateResendPendingEmailChangeCommand(
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange.ResendPendingEmailChangeCommand(
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation.SendEmailConfirmationCommand CreateSendEmailConfirmationCommand(
        string email = "neo@matrix.local",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation.SendEmailConfirmationCommand(
            Email: email,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommand CreateConfirmEmailCommand(
        Guid userId,
        string token = "raw-email-confirmation-token",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail.ConfirmEmailCommand(
            UserId: userId,
            Token: token,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery.RequestAccountRecoveryCommand CreateRequestAccountRecoveryCommand(
        string email = "neo@matrix.local",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery.RequestAccountRecoveryCommand(
            Email: email,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommand CreateConfirmAccountRecoveryCommand(
        Guid userId,
        string token = "raw-account-recovery-token",
        string? ipAddress = "127.0.0.1",
        string? userAgent = "Mozilla/5.0")
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommand(
            UserId: userId,
            Token: token,
            IpAddress: ipAddress,
            UserAgent: userAgent);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken.RefreshTokenCommandHandler CreateRefreshHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeAccessTokenService accessTokenService,
        FakeRefreshTokenProvider refreshTokenProvider,
        FakeGeoLocationService geoLocationService,
        FakeUnitOfWork unitOfWork,
        FakeEffectivePermissionsService permissionsService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken.RefreshTokenCommandHandler(
            userRepository,
            userSessionRepository,
            accessTokenService,
            refreshTokenProvider,
            geoLocationService,
            unitOfWork,
            new TestClock(),
            permissionsService);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken.RevokeRefreshTokenCommandHandler CreateRevokeRefreshHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeRefreshTokenProvider refreshTokenProvider,
        FakeUnitOfWork unitOfWork,
        FakeSecurityAuditService securityAuditService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken.RevokeRefreshTokenCommandHandler(
            userRepository,
            userSessionRepository,
            refreshTokenProvider,
            unitOfWork,
            new TestClock(),
            securityAuditService);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword.ResetPasswordCommandHandler CreateResetPasswordHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeOneTimeTokenRepository oneTimeTokenRepository,
        FakeOneTimeTokenService oneTimeTokenService,
        FakePasswordHasher passwordHasher,
        FakeUnitOfWork unitOfWork,
        FakeSecurityAuditService securityAuditService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword.ResetPasswordCommandHandler(
            userRepository,
            userSessionRepository,
            oneTimeTokenRepository,
            oneTimeTokenService,
            passwordHasher,
            new TestClock(),
            unitOfWork,
            securityAuditService);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession.RevokeMySessionCommandHandler CreateRevokeMySessionHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeUnitOfWork unitOfWork,
        FakeCurrentUserContext currentUser,
        FakeSecurityAuditService securityAuditService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession.RevokeMySessionCommandHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            new TestClock(),
            currentUser,
            securityAuditService);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions.RevokeOtherMySessionsCommandHandler CreateRevokeOtherMySessionsHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeUnitOfWork unitOfWork,
        FakeCurrentUserContext currentUser,
        FakeSecurityAuditService securityAuditService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions.RevokeOtherMySessionsCommandHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            new TestClock(),
            currentUser,
            securityAuditService);
    }

    internal static Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions.RevokeAllMySessionsCommandHandler CreateRevokeAllMySessionsHandler(
        FakeUserRepository userRepository,
        FakeUserSessionRepository userSessionRepository,
        FakeUnitOfWork unitOfWork,
        FakeCurrentUserContext currentUser,
        FakeSecurityAuditService securityAuditService)
    {
        return new Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions.RevokeAllMySessionsCommandHandler(
            userRepository,
            userSessionRepository,
            unitOfWork,
            new TestClock(),
            currentUser,
            securityAuditService);
    }

    internal static OneTimeToken CreateOneTimeToken(
        Guid userId,
        OneTimeTokenPurpose purpose = OneTimeTokenPurpose.PasswordReset,
        string tokenHash = "raw-reset-token-hash",
        DateTime? expiresAtUtc = null)
    {
        return OneTimeToken.Create(
            userId: userId,
            purpose: purpose,
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc ?? UtcNow.AddHours(1),
            createdAtUtc: UtcNow.AddMinutes(-10));
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public User? UserById { get; set; }
        public User? UserByIdWithRefreshTokens { get; set; }
        public User? UserByRefreshTokenHash { get; set; }
        public User? UserByEmail { get; set; }
        public User? UserByPendingEmail { get; set; }
        public string? RequestedRefreshTokenHash { get; private set; }
        public Guid? RequestedUserId { get; private set; }
        public string? RequestedEmail { get; private set; }
        public string? RequestedPendingEmail { get; private set; }

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

        public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            RequestedRefreshTokenHash = tokenHash;
            return Task.FromResult(UserByRefreshTokenHash);
        }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            RequestedEmail = normalizedEmail;
            return Task.FromResult(UserByEmail);
        }

        public Task<User?> GetByPendingEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            RequestedPendingEmail = normalizedEmail;
            return Task.FromResult(UserByPendingEmail);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> BumpPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> BumpPermissionsVersionByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Guid>> GetUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    internal sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public List<UserSession> Sessions { get; } = new();
        public Guid? RequestedSessionId { get; private set; }
        public Guid? RequestedUserId { get; private set; }
        public string? RequestedDeviceId { get; private set; }

        public Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            RequestedSessionId = sessionId;
            UserSession? session = Sessions.SingleOrDefault(x => x.Id == sessionId);
            return Task.FromResult(session);
        }

        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            IReadOnlyCollection<UserSession> items = Sessions
                .Where(x => x.UserId == userId)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task<IReadOnlyCollection<UserSession>> ListByUserIdAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
        {
            RequestedUserId = userId;
            RequestedDeviceId = deviceId;
            IReadOnlyCollection<UserSession> items = Sessions
                .Where(x => x.UserId == userId && x.DeviceInfo.DeviceId == deviceId)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<UserSession?> GetActiveByUserIdAndDeviceIdAsync(Guid userId, string deviceId, DateTime utcNow, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<UserSession> Items, int TotalCount)> GetEndedPageByUserIdAsync(Guid userId, DateTime utcNow, Pagination pagination, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DateTime?> GetLastVisitedAtUtcAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<UserSession>> ListActiveByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    internal sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordVerificationOutcome VerifyOutcome { get; set; } = PasswordVerificationOutcome.Success;
        public List<string> HashedPasswords { get; } = new();
        public List<(Guid UserId, string PasswordHash, string ProvidedPassword)> VerifyCalls { get; } = new();

        public string Hash(string password)
        {
            HashedPasswords.Add(password);
            return $"hash::{password}";
        }

        public PasswordVerificationOutcome Verify(User user, string passwordHash, string providedPassword)
        {
            VerifyCalls.Add((user.Id, passwordHash, providedPassword));
            return VerifyOutcome;
        }
    }

    internal sealed class FakeRefreshTokenProvider : IRefreshTokenProvider
    {
        public string ComputedHash { get; set; } = "incoming-refresh-token-hash";
        public List<string> ComputeHashInputs { get; } = new();
        public bool? RequestedIsPersistent { get; private set; }
        public RefreshTokenDescriptor Result { get; set; } = new(
            Token: "rotated-refresh-token",
            TokenHash: "rotated-refresh-token-hash",
            ExpiresAtUtc: RotatedRefreshTokenExpiresAtUtc);

        public string ComputeHash(string token)
        {
            ComputeHashInputs.Add(token);
            return ComputedHash;
        }

        public RefreshTokenDescriptor Generate(bool isPersistent)
        {
            RequestedIsPersistent = isPersistent;
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
        public List<SecurityAuditEntry> Entries { get; } = new();

        public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> IsLoginAllowedAsync(string loginSubject, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsAccountRecoveryRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailChangeRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsEmailConfirmationRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsPasswordResetRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeEmailSender : IEmailSender
    {
        public List<string> AccountDeletedEmails { get; } = new();
        public Exception? SendAccountDeletedException { get; set; }

        public Task SendAccountDeleted(string toEmail, CancellationToken cancellationToken)
        {
            AccountDeletedEmails.Add(toEmail);

            if (SendAccountDeletedException is not null)
                throw SendAccountDeletedException;

            return Task.CompletedTask;
        }

        public Task SendAccountRecovery(string toEmail, string recoveryLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendEmailChangeConfirmation(string toEmail, string currentEmail, string confirmationLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendEmailConfirmation(string toEmail, string confirmationLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendPasswordReset(string toEmail, string resetLink, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendUsernameChanged(string toEmail, string previousUsername, string newUsername, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SendAccountRestored(string toEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakePendingEmailChangeDeliveryService : IPendingEmailChangeDeliveryService
    {
        public List<(Guid UserId, string PendingEmail, string? IpAddress, string? UserAgent, SecurityAuditEventType EventType)> Requests { get; } = new();

        public Task SendConfirmationAsync(
            User user,
            string pendingEmail,
            string? ipAddress,
            string? userAgent,
            SecurityAuditEventType eventType,
            CancellationToken cancellationToken)
        {
            Requests.Add((user.Id, pendingEmail, ipAddress, userAgent, eventType));
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeOneTimeTokenRepository : IOneTimeTokenRepository
    {
        public OneTimeToken? FoundToken { get; set; }
        public (Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)? FindRequest { get; private set; }
        public IReadOnlyList<OneTimeToken> ActiveTokens { get; set; } = Array.Empty<OneTimeToken>();
        public (Guid UserId, OneTimeTokenPurpose Purpose, DateTime NowUtc)? GetActiveRequest { get; private set; }

        public Task<OneTimeToken?> Find(Guid userId, OneTimeTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken)
        {
            FindRequest = (userId, purpose, tokenHash);
            return Task.FromResult(FoundToken);
        }

        public Task Add(OneTimeToken token, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> CountCreatedSinceUtc(Guid userId, OneTimeTokenPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<OneTimeToken>> GetActive(Guid userId, OneTimeTokenPurpose purpose, DateTime nowUtc, CancellationToken cancellationToken)
        {
            GetActiveRequest = (userId, purpose, nowUtc);
            return Task.FromResult(ActiveTokens);
        }

        public Task<DateTime?> GetLatestCreatedAtUtc(Guid userId, OneTimeTokenPurpose purpose, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeOneTimeTokenService : IOneTimeTokenService
    {
        public string HashedToken { get; set; } = "raw-reset-token-hash";
        public List<string> HashTokenInputs { get; } = new();

        public string HashToken(string rawToken)
        {
            HashTokenInputs.Add(rawToken);
            return HashedToken;
        }

        public string GenerateRawToken() => throw new NotSupportedException();
        public TimeSpan GetDeliveryCooldown(OneTimeTokenPurpose purpose) => throw new NotSupportedException();
        public int GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose purpose) => throw new NotSupportedException();
        public TimeSpan GetTtl(OneTimeTokenPurpose purpose) => throw new NotSupportedException();
    }

    internal sealed class FakeOneTimeTokenDeliveryService : IOneTimeTokenDeliveryService
    {
        public List<(string Email, string? IpAddress, string? UserAgent)> PasswordResetRequests { get; } = new();
        public List<(string Email, string? IpAddress, string? UserAgent)> EmailConfirmationRequests { get; } = new();
        public List<(string Email, string? IpAddress, string? UserAgent)> AccountRecoveryRequests { get; } = new();

        public Task SendPasswordResetAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            PasswordResetRequests.Add((email, ipAddress, userAgent));
            return Task.CompletedTask;
        }

        public Task SendAccountRecoveryAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            AccountRecoveryRequests.Add((email, ipAddress, userAgent));
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(string email, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        {
            EmailConfirmationRequests.Add((email, ipAddress, userAgent));
            return Task.CompletedTask;
        }
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

    internal sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid? UserId { get; set; }
        public Guid? SessionId { get; set; }
    }

    internal sealed class TestClock : IClock
    {
        public DateTime UtcNow => SelfServiceHandlerTestSupport.UtcNow;
    }
}
