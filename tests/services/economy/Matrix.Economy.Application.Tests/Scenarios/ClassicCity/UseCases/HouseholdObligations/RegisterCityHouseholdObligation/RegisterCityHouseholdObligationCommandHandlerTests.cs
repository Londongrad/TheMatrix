using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RegisterCityHouseholdObligation;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdObligations.RegisterCityHouseholdObligation
{
    public sealed class RegisterCityHouseholdObligationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_CreatesObligationWithDefaultDueDateFromFrozenClock()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant",
                openingBalance: 200m);
            CityBusiness providerBusiness = CreateBusiness(
                cityId: cityId,
                name: "Landlord",
                kind: CityBusinessKind.Landlord,
                initialCapital: 500m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var obligationRepository = new FakeCityHouseholdObligationRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 18,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityHouseholdObligationCommandHandler(
                householdAccountRepository: householdAccountRepository,
                businessRepository: businessRepository,
                obligationRepository: obligationRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityHouseholdObligationCommand(
                CityId: cityId,
                HouseholdAccountId: householdAccount.Id,
                ProviderBusinessId: providerBusiness.Id,
                Name: "Monthly Rent",
                Kind: CityHouseholdObligationKind.Rent,
                BillingCadence: CityHouseholdObligationBillingCadence.Monthly,
                ChargeAmount: 80m,
                TaxAmount: 8m,
                FirstChargeDueAtUtc: null);

            CityHouseholdObligationDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityHouseholdObligation obligation = Assert.Single(obligationRepository.AddedObligations);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: obligation.CreatedAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow.AddMonths(1),
                actual: obligation.NextChargeDueAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow.AddMonths(1)
                   .ToString("O"),
                actual: result.NextChargeDueAtUtc);
            Assert.Equal(
                expected: "Rent",
                actual: result.Kind);
            Assert.Equal(
                expected: "Monthly",
                actual: result.BillingCadence);
            Assert.Equal(
                expected: 80m,
                actual: result.ChargeAmount);
            Assert.Equal(
                expected: 8m,
                actual: result.TaxAmount);
            Assert.Equal(
                expected: householdAccount.Id,
                actual: result.HouseholdAccountId);
            Assert.Equal(
                expected: providerBusiness.Id,
                actual: result.ProviderBusinessId);
        }

        [Fact]
        public async Task Handle_ThrowsWhenActorsBelongToDifferentCities()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant",
                openingBalance: 200m);
            CityBusiness providerBusiness = CreateBusiness(
                cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                name: "Regional Utility",
                kind: CityBusinessKind.Utility,
                initialCapital: 500m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var obligationRepository = new FakeCityHouseholdObligationRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 18,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RegisterCityHouseholdObligationCommandHandler(
                householdAccountRepository: householdAccountRepository,
                businessRepository: businessRepository,
                obligationRepository: obligationRepository,
                unitOfWork: unitOfWork,
                timeProvider: timeProvider);
            var command = new RegisterCityHouseholdObligationCommand(
                CityId: cityId,
                HouseholdAccountId: householdAccount.Id,
                ProviderBusinessId: providerBusiness.Id,
                Name: "Cross-city Utility",
                Kind: CityHouseholdObligationKind.Utilities,
                BillingCadence: CityHouseholdObligationBillingCadence.Monthly,
                ChargeAmount: 40m,
                TaxAmount: 4m,
                FirstChargeDueAtUtc: null);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => handler.Handle(
                    request: command,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Obligation actors must belong to the same city.",
                actual: exception.Message);
            Assert.Empty(obligationRepository.AddedObligations);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
