using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName
{
    public sealed class ChangeDisplayNameCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<ChangeDisplayNameCommand, string?>
    {
        public async Task<string?> Handle(
            ChangeDisplayNameCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            if (user.IsDeleted)
                throw ApplicationErrorsFactory.AccountDeleted();

            user.ChangeDisplayName(request.DisplayName);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return user.DisplayName;
        }
    }
}
