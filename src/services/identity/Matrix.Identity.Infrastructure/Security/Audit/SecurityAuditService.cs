using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Security.Audit
{
    public sealed class SecurityAuditService(
        IdentityDbContext dbContext,
        IClock clock,
        IOptions<SecurityAuditOptions> options) : ISecurityAuditService
    {
        private readonly SecurityAuditOptions _options = options.Value;

        public async Task WriteAsync(
            SecurityAuditEntry entry,
            CancellationToken cancellationToken)
        {
            SecurityAuditEventRecord auditEvent = SecurityAuditEventRecord.Create(
                entry: entry,
                occurredAtUtc: clock.UtcNow);

            await dbContext.AddAsync(
                entity: auditEvent,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> IsLoginAllowedAsync(
            string loginSubject,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            DateTime sinceUtc = clock.UtcNow.AddMinutes(-_options.FailedLoginWindowMinutes);

            if (_options.FailedLoginMaxAttemptsPerLogin > 0)
            {
                int failuresByLogin = await CountRecentAsync(
                    eventType: SecurityAuditEventType.Login,
                    isSuccessful: false,
                    subject: loginSubject,
                    ipAddress: null,
                    sinceUtc: sinceUtc,
                    cancellationToken: cancellationToken);

                if (failuresByLogin >= _options.FailedLoginMaxAttemptsPerLogin)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress) &&
                _options.FailedLoginMaxAttemptsPerIp > 0)
            {
                int failuresByIp = await CountRecentAsync(
                    eventType: SecurityAuditEventType.Login,
                    isSuccessful: false,
                    subject: null,
                    ipAddress: ipAddress,
                    sinceUtc: sinceUtc,
                    cancellationToken: cancellationToken);

                if (failuresByIp >= _options.FailedLoginMaxAttemptsPerIp)
                    return false;
            }

            return true;
        }

        public Task<bool> IsEmailConfirmationRequestAllowedAsync(
            string normalizedEmail,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            return IsRequestAllowedAsync(
                eventType: SecurityAuditEventType.EmailConfirmationRequested,
                subject: normalizedEmail,
                ipAddress: ipAddress,
                windowMinutes: _options.EmailConfirmationRequestWindowMinutes,
                maxAttemptsPerSubject: _options.EmailConfirmationRequestMaxAttemptsPerEmail,
                maxAttemptsPerIp: _options.EmailConfirmationRequestMaxAttemptsPerIp,
                cancellationToken: cancellationToken);
        }

        public Task<bool> IsPasswordResetRequestAllowedAsync(
            string normalizedEmail,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            return IsRequestAllowedAsync(
                eventType: SecurityAuditEventType.PasswordResetRequested,
                subject: normalizedEmail,
                ipAddress: ipAddress,
                windowMinutes: _options.PasswordResetRequestWindowMinutes,
                maxAttemptsPerSubject: _options.PasswordResetRequestMaxAttemptsPerEmail,
                maxAttemptsPerIp: _options.PasswordResetRequestMaxAttemptsPerIp,
                cancellationToken: cancellationToken);
        }

        private async Task<bool> IsRequestAllowedAsync(
            SecurityAuditEventType eventType,
            string subject,
            string? ipAddress,
            int windowMinutes,
            int maxAttemptsPerSubject,
            int maxAttemptsPerIp,
            CancellationToken cancellationToken)
        {
            DateTime sinceUtc = clock.UtcNow.AddMinutes(-windowMinutes);

            if (maxAttemptsPerSubject > 0)
            {
                int attemptsBySubject = await CountRecentAsync(
                    eventType: eventType,
                    isSuccessful: null,
                    subject: subject,
                    ipAddress: null,
                    sinceUtc: sinceUtc,
                    cancellationToken: cancellationToken);

                if (attemptsBySubject >= maxAttemptsPerSubject)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress) && maxAttemptsPerIp > 0)
            {
                int attemptsByIp = await CountRecentAsync(
                    eventType: eventType,
                    isSuccessful: null,
                    subject: null,
                    ipAddress: ipAddress,
                    sinceUtc: sinceUtc,
                    cancellationToken: cancellationToken);

                if (attemptsByIp >= maxAttemptsPerIp)
                    return false;
            }

            return true;
        }

        private async Task<int> CountRecentAsync(
            SecurityAuditEventType eventType,
            bool? isSuccessful,
            string? subject,
            string? ipAddress,
            DateTime sinceUtc,
            CancellationToken cancellationToken)
        {
            IQueryable<SecurityAuditEventRecord> query = dbContext.Set<SecurityAuditEventRecord>()
                .AsNoTracking()
                .Where(x => x.EventType == eventType)
                .Where(x => x.OccurredAtUtc >= sinceUtc);

            if (isSuccessful.HasValue)
                query = query.Where(x => x.IsSuccessful == isSuccessful.Value);

            if (!string.IsNullOrWhiteSpace(subject))
                query = query.Where(x => x.Subject == subject);

            if (!string.IsNullOrWhiteSpace(ipAddress))
                query = query.Where(x => x.IpAddress == ipAddress);

            return await query.CountAsync(cancellationToken);
        }
    }
}
