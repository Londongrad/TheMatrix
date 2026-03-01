using Matrix.Identity.Application.Abstractions.Services;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery
{
    public sealed class RequestAccountRecoveryCommandHandler(
        IOneTimeTokenDeliveryService oneTimeTokenDeliveryService) : IRequestHandler<RequestAccountRecoveryCommand>
    {
        public async Task Handle(
            RequestAccountRecoveryCommand request,
            CancellationToken cancellationToken)
        {
            await oneTimeTokenDeliveryService.SendAccountRecoveryAsync(
                email: request.Email,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                cancellationToken: cancellationToken);
        }
    }
}
