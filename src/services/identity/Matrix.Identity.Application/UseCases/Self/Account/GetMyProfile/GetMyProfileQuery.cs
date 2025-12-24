using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<MyProfileResult>;
}
