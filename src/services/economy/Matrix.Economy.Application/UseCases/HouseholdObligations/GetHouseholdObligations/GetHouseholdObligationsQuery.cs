using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations
{
    public sealed record GetHouseholdObligationsQuery(Guid HouseholdAccountId)
        : IRequest<IReadOnlyList<CityHouseholdObligationDto>>;
}
