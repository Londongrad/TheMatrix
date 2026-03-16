using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar
{
    public sealed class ClearAvatarCommandHandler(
        IUserRepository userRepository,
        IAvatarStorage avatarStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<ClearAvatarCommand, string?>
    {
        public async Task<string?> Handle(
            ClearAvatarCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                await avatarStorage.DeleteAsync(
                    path: user.AvatarUrl,
                    cancellationToken: cancellationToken);

            user.ChangeAvatar(null);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return null;
        }
    }
}
