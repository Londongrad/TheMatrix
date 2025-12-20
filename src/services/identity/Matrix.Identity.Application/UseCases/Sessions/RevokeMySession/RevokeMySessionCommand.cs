using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeMySession
{
    public sealed record RevokeMySessionCommand(Guid SessionId) : IRequest;
}
