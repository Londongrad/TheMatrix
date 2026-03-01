using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IPendingEmailChangeDeliveryService
    {
        Task SendConfirmationAsync(
            User user,
            string pendingEmail,
            string? ipAddress,
            string? userAgent,
            SecurityAuditEventType eventType,
            CancellationToken cancellationToken);
    }
}
