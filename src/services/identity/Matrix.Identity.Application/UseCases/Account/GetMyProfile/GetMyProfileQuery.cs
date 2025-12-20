using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<MyProfileResult>;
}
