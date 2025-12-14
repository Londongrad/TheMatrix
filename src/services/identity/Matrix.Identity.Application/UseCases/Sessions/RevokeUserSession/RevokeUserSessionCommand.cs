using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeUserSession
{
    public sealed record RevokeUserSessionCommand(
        Guid UserId,
        Guid SessionId) : IRequest;
}
