using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed record GetMySecurityActivityQuery(int Limit)
        : IRequest<IReadOnlyCollection<SecurityActivityItemResult>>;
}
