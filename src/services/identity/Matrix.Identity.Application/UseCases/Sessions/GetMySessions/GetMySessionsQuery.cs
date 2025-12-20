using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.GetMySessions
{
    public sealed record GetMySessionsQuery
        : IRequest<IReadOnlyCollection<MySessionResult>>;
}
