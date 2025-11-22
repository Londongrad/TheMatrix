using MediatR;

namespace Matrix.Identity.Application.UseCases.LoginUser
{
    public sealed record LoginUserCommand(
        string Login,
        string Password
    ) : IRequest<LoginUserResult>;
}
