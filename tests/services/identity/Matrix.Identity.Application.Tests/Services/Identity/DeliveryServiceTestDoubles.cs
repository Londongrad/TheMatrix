using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Tests.Services.Identity;

internal sealed class DeliveryFakeUserRepository : IUserRepository
{
    public User? UserByEmail { get; set; }
    public string? RequestedNormalizedEmail { get; private set; }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        RequestedNormalizedEmail = normalizedEmail;
        return Task.FromResult(UserByEmail);
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
    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<int?> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public IAsyncEnumerable<Guid> StreamUserIdsByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> IsUsernameTakenAsync(string normalizedUsername, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class DeliveryFakeOneTimeTokenRepository : IOneTimeTokenRepository
{
    public DateTime? LatestCreatedAtUtc { get; set; }
    public int RecentAttempts { get; set; }
    public List<OneTimeToken> ActiveTokens { get; } = new();
    public List<OneTimeToken> AddedTokens { get; } = new();
    public (Guid UserId, OneTimeTokenPurpose Purpose, DateTime SinceUtc)? CountCreatedSinceUtcRequest { get; private set; }
    public (Guid UserId, OneTimeTokenPurpose Purpose, DateTime NowUtc)? GetActiveRequest { get; private set; }
    public (Guid UserId, OneTimeTokenPurpose Purpose)? GetLatestCreatedAtUtcRequest { get; private set; }

    public Task Add(OneTimeToken token, CancellationToken cancellationToken)
    {
        AddedTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<int> CountCreatedSinceUtc(
        Guid userId,
        OneTimeTokenPurpose purpose,
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        CountCreatedSinceUtcRequest = (userId, purpose, sinceUtc);
        return Task.FromResult(RecentAttempts);
    }

    public Task<IReadOnlyList<OneTimeToken>> GetActive(
        Guid userId,
        OneTimeTokenPurpose purpose,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        GetActiveRequest = (userId, purpose, nowUtc);
        return Task.FromResult<IReadOnlyList<OneTimeToken>>(ActiveTokens);
    }

    public Task<DateTime?> GetLatestCreatedAtUtc(
        Guid userId,
        OneTimeTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        GetLatestCreatedAtUtcRequest = (userId, purpose);
        return Task.FromResult(LatestCreatedAtUtc);
    }

    public Task<OneTimeToken?> Find(
        Guid userId,
        OneTimeTokenPurpose purpose,
        string tokenHash,
        CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class DeliveryFakeOneTimeTokenService : IOneTimeTokenService
{
    public string RawToken { get; set; } = "raw-token";
    public string HashedToken { get; set; } = "hashed-token";
    public TimeSpan DeliveryCooldown { get; set; }
    public int MaxDeliveryAttemptsPerHour { get; set; } = 5;
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(1);
    public List<string> HashTokenInputs { get; } = new();

    public string GenerateRawToken()
    {
        return RawToken;
    }

    public TimeSpan GetDeliveryCooldown(OneTimeTokenPurpose purpose)
    {
        return DeliveryCooldown;
    }

    public int GetMaxDeliveryAttemptsPerHour(OneTimeTokenPurpose purpose)
    {
        return MaxDeliveryAttemptsPerHour;
    }

    public TimeSpan GetTtl(OneTimeTokenPurpose purpose)
    {
        return Ttl;
    }

    public string HashToken(string rawToken)
    {
        HashTokenInputs.Add(rawToken);
        return HashedToken;
    }
}

internal sealed class DeliveryFakeEmailSender : IEmailSender
{
    public List<(string ToEmail, string ResetLink)> PasswordResetEmails { get; } = new();
    public List<(string ToEmail, string CurrentEmail, string ConfirmationLink)> EmailChangeConfirmationEmails { get; } = new();

    public Task SendEmailChangeConfirmation(
        string toEmail,
        string currentEmail,
        string confirmationLink,
        CancellationToken cancellationToken)
    {
        EmailChangeConfirmationEmails.Add((toEmail, currentEmail, confirmationLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordReset(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken)
    {
        PasswordResetEmails.Add((toEmail, resetLink));
        return Task.CompletedTask;
    }

    public Task SendAccountDeleted(string toEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task SendAccountRecovery(string toEmail, string recoveryLink, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task SendAccountRestored(string toEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task SendEmailConfirmation(string toEmail, string confirmationLink, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task SendUsernameChanged(string toEmail, string previousUsername, string newUsername, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class DeliveryFakeFrontendLinkBuilder : IFrontendLinkBuilder
{
    public string ResetPasswordLink { get; set; } = "https://matrix.local/reset-password";
    public string ConfirmEmailChangeLink { get; set; } = "https://matrix.local/confirm-email-change";
    public (Guid UserId, string RawToken)? ResetPasswordLinkRequest { get; private set; }
    public (Guid UserId, string RawToken)? ConfirmEmailChangeLinkRequest { get; private set; }

    public string BuildConfirmEmailChangeLink(Guid userId, string rawToken)
    {
        ConfirmEmailChangeLinkRequest = (userId, rawToken);
        return ConfirmEmailChangeLink;
    }

    public string BuildResetPasswordLink(Guid userId, string rawToken)
    {
        ResetPasswordLinkRequest = (userId, rawToken);
        return ResetPasswordLink;
    }

    public string BuildAccountRecoveryLink(Guid userId, string rawToken) => throw new NotSupportedException();
    public string BuildConfirmEmailLink(Guid userId, string rawToken) => throw new NotSupportedException();
}

internal sealed class DeliveryFakeSecurityAuditService : ISecurityAuditService
{
    public bool PasswordResetRequestAllowed { get; set; } = true;
    public bool EmailChangeRequestAllowed { get; set; } = true;
    public List<SecurityAuditEntry> Entries { get; } = new();
    public (string NormalizedEmail, string? IpAddress)? PasswordResetAllowedRequest { get; private set; }
    public (string NormalizedEmail, string? IpAddress)? EmailChangeAllowedRequest { get; private set; }

    public Task<bool> IsEmailChangeRequestAllowedAsync(
        string normalizedEmail,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        EmailChangeAllowedRequest = (normalizedEmail, ipAddress);
        return Task.FromResult(EmailChangeRequestAllowed);
    }

    public Task<bool> IsPasswordResetRequestAllowedAsync(
        string normalizedEmail,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        PasswordResetAllowedRequest = (normalizedEmail, ipAddress);
        return Task.FromResult(PasswordResetRequestAllowed);
    }

    public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<bool> IsAccountRecoveryRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<bool> IsEmailConfirmationRequestAllowedAsync(string normalizedEmail, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<bool> IsLoginAllowedAsync(string loginSubject, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class DeliveryFakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();
}
