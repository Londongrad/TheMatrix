using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RegisterCityHouseholdObligation
{
    public sealed class RegisterCityHouseholdObligationCommandHandler(
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityBusinessRepository businessRepository,
        ICityHouseholdObligationRepository obligationRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RegisterCityHouseholdObligationCommand, CityHouseholdObligationDto>
    {
        public async Task<CityHouseholdObligationDto> Handle(
            RegisterCityHouseholdObligationCommand request,
            CancellationToken cancellationToken)
        {
            DateTimeOffset createdAtUtc = timeProvider.GetUtcNow();

            CityHouseholdAccount householdAccount =
                await householdAccountRepository.GetByIdAsync(
                    householdAccountId: request.HouseholdAccountId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");
            CityBusiness providerBusiness =
                await businessRepository.GetByIdAsync(
                    businessId: request.ProviderBusinessId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Business '{request.ProviderBusinessId}' was not found.");

            if (householdAccount.CityId != request.CityId || providerBusiness.CityId != request.CityId)
                throw new InvalidOperationException("Obligation actors must belong to the same city.");

            providerBusiness.EnsureCompatibleUnit(householdAccount.GetUnitProfile());
            providerBusiness.EnsureCanServeObligation(request.Kind);

            DateTimeOffset firstChargeDueAtUtc = request.FirstChargeDueAtUtc ??
                                                 request.BillingCadence switch
                                                 {
                                                     CityHouseholdObligationBillingCadence.Daily =>
                                                         createdAtUtc.AddDays(1),
                                                     CityHouseholdObligationBillingCadence.Weekly => createdAtUtc
                                                        .AddDays(7),
                                                     _ => createdAtUtc.AddMonths(1)
                                                 };

            var obligation = new CityHouseholdObligation(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: request.Name,
                kind: request.Kind,
                billingCadence: request.BillingCadence,
                createdAtUtc: createdAtUtc,
                firstChargeDueAtUtc: firstChargeDueAtUtc,
                unitProfile: householdAccount.GetUnitProfile(),
                chargeAmount: Money.FromDecimal(request.ChargeAmount),
                taxAmount: Money.FromDecimal(request.TaxAmount));

            obligationRepository.Add(obligation);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return GetCityHouseholdObligationsQueryHandler.Map(obligation);
        }
    }
}
