using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations
{
    public sealed class GetCityHouseholdObligationsQueryHandler(ICityHouseholdObligationRepository obligationRepository)
        : IRequestHandler<GetCityHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>
    {
        public async Task<IReadOnlyList<CityHouseholdObligationDto>> Handle(
            GetCityHouseholdObligationsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligation> obligations = await obligationRepository.ListByCityAsync(
                request.CityId,
                cancellationToken);

            return obligations.Select(Map).ToArray();
        }

        internal static CityHouseholdObligationDto Map(CityHouseholdObligation obligation)
        {
            return new CityHouseholdObligationDto(
                ObligationId: obligation.Id,
                CityId: obligation.CityId,
                HouseholdAccountId: obligation.HouseholdAccountId,
                ProviderBusinessId: obligation.ProviderBusinessId,
                CreatedAtUtc: obligation.CreatedAtUtc.ToString("O"),
                Name: obligation.Name,
                Kind: obligation.Kind.ToString(),
                IsActive: obligation.IsActive,
                UnitKind: obligation.UnitKind.ToString(),
                UnitCode: obligation.UnitCode,
                UnitDisplayName: obligation.UnitDisplayName,
                UnitSymbol: obligation.UnitSymbol,
                ChargeAmount: obligation.ChargeAmount.Amount,
                TaxAmount: obligation.TaxAmount.Amount,
                LastChargedAtUtc: obligation.LastChargedAtUtc?.ToString("O"),
                ChargeCount: obligation.ChargeCount);
        }
    }
}
