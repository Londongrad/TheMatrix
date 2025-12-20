using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ConfirmEmail
{
    public sealed class ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IClock clock,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<ConfirmEmailCommand>
    {
        public async Task Handle(
            ConfirmEmailCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            // For confirmation flow it's OK to treat "user not found" as invalid token.
            User? user = await userRepository.GetByIdAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            if (user is null)
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(userId));

            if (user.IsEmailConfirmed)
                return; // idempotent

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                tokenHash: tokenHash,
                cancellationToken: cancellationToken);

            if (token is null)
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.Token));

            DateTime nowUtc = clock.UtcNow;

            token.MarkUsed(nowUtc);
            user.ConfirmEmail();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
