using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Services;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation
{
    public sealed class SendEmailConfirmationCommandHandler(
        IOneTimeTokenDeliveryService oneTimeTokenDeliveryService) : IRequestHandler<SendEmailConfirmationCommand>
    {
        public async Task Handle(
            SendEmailConfirmationCommand request,
            CancellationToken cancellationToken)
        {
            await oneTimeTokenDeliveryService.SendEmailConfirmationAsync(
                email: request.Email,
                cancellationToken: cancellationToken);
        }
    }
}
