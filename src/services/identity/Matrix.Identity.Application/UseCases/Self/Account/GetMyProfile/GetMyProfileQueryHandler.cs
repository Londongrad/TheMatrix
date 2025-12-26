using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile
{
    public sealed class GetMyProfileQueryHandler(
        IUserRepository userRepository,
        ICurrentUserContext currentUser)
        : IRequestHandler<GetMyProfileQuery, MyProfileResult>
    {
        public async Task<MyProfileResult> Handle(
            GetMyProfileQuery request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            return new MyProfileResult
            {
                UserId = user.Id,
                Email = user.Email.Value,
                Username = user.Username.Value,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}
