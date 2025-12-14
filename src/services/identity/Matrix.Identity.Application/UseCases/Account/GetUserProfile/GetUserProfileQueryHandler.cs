using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.GetUserProfile
{
    public sealed class GetUserProfileQueryHandler(IUserRepository userRepository)
        : IRequestHandler<GetUserProfileQuery, UserProfileResult>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<UserProfileResult> Handle(
            GetUserProfileQuery request,
            CancellationToken cancellationToken)
        {
            User user =
                await _userRepository.GetByIdAsync(
                    userId: request.UserId,
                    cancellationToken: cancellationToken) ??
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return new UserProfileResult
            {
                UserId = user.Id,
                Email = user.Email.Value,
                Username = user.Username.Value,
                AvatarUrl = user.AvatarUrl
            };
        }
    }
}
