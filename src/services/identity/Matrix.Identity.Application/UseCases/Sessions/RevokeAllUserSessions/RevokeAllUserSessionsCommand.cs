using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeAllUserSessions
{
    public sealed record RevokeAllUserSessionsCommand(Guid UserId) : IRequest;
}
