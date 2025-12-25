using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails
{
    public sealed class GetUserDetailsQueryHandler(IUserRepository userRepository)
        : IRequestHandler<GetUserDetailsQuery, UserDetailsResult>
    {
        public async Task<UserDetailsResult> Handle(
            GetUserDetailsQuery request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return new UserDetailsResult
            {
                Id = user.Id,
                AvatarUrl = user.AvatarUrl,
                Username = user.Username.Value,
                Email = user.Email.Value,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsLocked = user.IsLocked,
                PermissionsVersion = user.PermissionsVersion,
                CreatedAtUtc = user.CreatedAtUtc
            };
        }
    }
}
