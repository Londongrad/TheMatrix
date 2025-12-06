using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.GetUserSessions
{
    public sealed record GetUserSessionsQuery(Guid UserId)
        : IRequest<IReadOnlyCollection<UserSessionResult>>;
}
