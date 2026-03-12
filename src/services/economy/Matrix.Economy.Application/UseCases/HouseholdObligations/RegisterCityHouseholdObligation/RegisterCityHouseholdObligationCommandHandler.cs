using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RegisterCityHouseholdObligation
{
    public sealed class RegisterCityHouseholdObligationCommandHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityBusinessRepository businessRepository,
        ICityHouseholdObligationRepository obligationRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RegisterCityHouseholdObligationCommand, CityHouseholdObligationDto>
    {
        public async Task<CityHouseholdObligationDto> Handle(
            RegisterCityHouseholdObligationCommand request,
            CancellationToken cancellationToken)
        {
            CityHouseholdAccount householdAccount = await householdAccountRepository.GetByIdAsync(request.HouseholdAccountId, cancellationToken)
                ?? throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");
            CityBusiness providerBusiness = await businessRepository.GetByIdAsync(request.ProviderBusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.ProviderBusinessId}' was not found.");

            if (householdAccount.CityId != request.CityId || providerBusiness.CityId != request.CityId)
            {
                throw new InvalidOperationException("Obligation actors must belong to the same city.");
            }

            providerBusiness.EnsureCompatibleUnit(householdAccount.GetUnitProfile());

            var obligation = new CityHouseholdObligation(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: request.Name,
                kind: request.Kind,
                createdAtUtc: DateTimeOffset.UtcNow,
                unitProfile: householdAccount.GetUnitProfile(),
                chargeAmount: Money.FromDecimal(request.ChargeAmount),
                taxAmount: Money.FromDecimal(request.TaxAmount));

            obligationRepository.Add(obligation);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return GetCityHouseholdObligations.GetCityHouseholdObligationsQueryHandler.Map(obligation);
        }
    }
}
