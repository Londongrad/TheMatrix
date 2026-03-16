using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations
{
    public sealed class GetHouseholdObligationsQueryHandler(ICityHouseholdObligationRepository obligationRepository)
        : IRequestHandler<GetHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>
    {
        public async Task<IReadOnlyList<CityHouseholdObligationDto>> Handle(
            GetHouseholdObligationsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligation> obligations = await obligationRepository.ListByHouseholdAsync(
                householdAccountId: request.HouseholdAccountId,
                cancellationToken: cancellationToken);

            return obligations.Select(GetCityHouseholdObligationsQueryHandler.Map)
               .ToArray();
        }
    }
}
