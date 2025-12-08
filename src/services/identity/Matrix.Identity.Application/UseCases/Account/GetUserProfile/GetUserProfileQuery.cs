using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.GetUserProfile
{
    public sealed record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileResult>;
}
