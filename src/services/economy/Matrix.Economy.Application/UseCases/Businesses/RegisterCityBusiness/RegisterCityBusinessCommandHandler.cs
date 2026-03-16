using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.RegisterCityBusiness
{
    public sealed class RegisterCityBusinessCommandHandler(
        ICityBusinessRepository businessRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RegisterCityBusinessCommand, CityBusinessDto>
    {
        public async Task<CityBusinessDto> Handle(
            RegisterCityBusinessCommand request,
            CancellationToken cancellationToken)
        {
            CityBudgetUnitProfile unitProfile = ResolveRequestedUnit(request);

            var business = new CityBusiness(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                name: request.Name,
                externalReferenceCode: null,
                templateKey: null,
                kind: request.Kind,
                createdAtUtc: DateTimeOffset.UtcNow,
                unitProfile: unitProfile,
                initialCapital: Money.FromDecimal(request.StartingCapital));

            businessRepository.Add(business);
            await unitOfWork.SaveChangesAsync(cancellationToken);

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

        private static CityBudgetUnitProfile ResolveRequestedUnit(RegisterCityBusinessCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.UnitCode) &&
                string.IsNullOrWhiteSpace(request.UnitDisplayName) &&
                string.IsNullOrWhiteSpace(request.UnitSymbol) &&
                string.IsNullOrWhiteSpace(request.UnitKind))
                return CityBudgetUnitProfile.DefaultMoney();

            if (!Enum.TryParse(
                    value: request.UnitKind,
                    ignoreCase: true,
                    result: out CityBudgetUnitKind unitKind))
                throw new InvalidOperationException($"Unsupported unit kind '{request.UnitKind}'.");

            return new CityBudgetUnitProfile(
                Kind: unitKind,
                Code: request.UnitCode ?? string.Empty,
                DisplayName: request.UnitDisplayName ?? string.Empty,
                Symbol: request.UnitSymbol ?? string.Empty);
        }
    }
}
