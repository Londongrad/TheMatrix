using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations
{
    public sealed record GetCityHouseholdObligationsQuery(Guid CityId) : IRequest<IReadOnlyList<CityHouseholdObligationDto>>;
}
