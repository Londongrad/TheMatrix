using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations
{
    public sealed class GetCityHouseholdObligationsQueryHandler(ICityHouseholdObligationRepository obligationRepository)
        : IRequestHandler<GetCityHouseholdObligationsQuery, IReadOnlyList<CityHouseholdObligationDto>>
    {
        public async Task<IReadOnlyList<CityHouseholdObligationDto>> Handle(
            GetCityHouseholdObligationsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligation> obligations = await obligationRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            return obligations.Select(Map)
               .ToArray();
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
                BillingCadence: obligation.BillingCadence.ToString(),
                NextChargeDueAtUtc: obligation.NextChargeDueAtUtc.ToString("O"),
                LastChargedAtUtc: obligation.LastChargedAtUtc?.ToString("O"),
                ChargeCount: obligation.ChargeCount);
        }
    }
}
