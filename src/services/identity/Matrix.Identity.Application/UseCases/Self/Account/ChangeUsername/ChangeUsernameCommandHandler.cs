using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername
{
    public sealed class ChangeUsernameCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<ChangeUsernameCommand, string>
    {
        public async Task<string> Handle(
            ChangeUsernameCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            Username newUsername = Username.Create(request.Username);

            if (string.Equals(
                    a: user.Username.Value,
                    b: newUsername.Value,
                    comparisonType: StringComparison.Ordinal))
                return user.Username.Value;

            bool isTaken = await userRepository.IsUsernameTakenAsync(
                normalizedUsername: newUsername.Value,
                cancellationToken: cancellationToken);

            if (isTaken)
                throw ApplicationErrorsFactory.UsernameAlreadyInUse(newUsername.Value);

            user.ChangeUsername(newUsername);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return user.Username.Value;
        }
    }
}
