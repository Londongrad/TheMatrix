using Matrix.Identity.Application.Abstractions.Services;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset
{
    public sealed class SendPasswordResetCommandHandler(
        IOneTimeTokenDeliveryService oneTimeTokenDeliveryService) : IRequestHandler<SendPasswordResetCommand>
    {
        public async Task Handle(
            SendPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            await oneTimeTokenDeliveryService.SendPasswordResetAsync(
                email: request.Email,
                cancellationToken: cancellationToken);
        }
    }
}
