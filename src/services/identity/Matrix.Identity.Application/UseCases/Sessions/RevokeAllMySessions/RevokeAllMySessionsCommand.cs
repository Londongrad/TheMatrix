using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeAllMySessions
{
    public sealed record RevokeAllMySessionsCommand : IRequest;
}
