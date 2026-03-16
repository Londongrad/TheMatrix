using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinesses
{
    public sealed class GetCityBusinessesQueryHandler(ICityBusinessRepository businessRepository)
        : IRequestHandler<GetCityBusinessesQuery, IReadOnlyList<CityBusinessDto>>
    {
        public async Task<IReadOnlyList<CityBusinessDto>> Handle(
            GetCityBusinessesQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            return businesses.Select(Map)
               .ToArray();
        }

        private static CityBusinessDto Map(CityBusiness business)
        {
            return new CityBusinessDto(
                BusinessId: business.Id,
                CityId: business.CityId,
                CreatedAtUtc: business.CreatedAtUtc.ToString("O"),
                Name: business.Name,
                Kind: business.Kind.ToString(),
                UnitKind: business.UnitKind.ToString(),
                UnitCode: business.UnitCode,
                UnitDisplayName: business.UnitDisplayName,
                UnitSymbol: business.UnitSymbol,
                Balance: business.Balance.Amount,
                TaxReserve: business.TaxReserve.Amount,
                TotalCapitalInjections: business.TotalCapitalInjections.Amount,
                TotalRetailTurnover: business.TotalRetailTurnover.Amount,
                TotalNetSalesRevenue: business.TotalNetSalesRevenue.Amount,
                TotalOperatingExpenses: business.TotalOperatingExpenses.Amount,
                TotalTaxRemitted: business.TotalTaxRemitted.Amount);
        }
    }
}
