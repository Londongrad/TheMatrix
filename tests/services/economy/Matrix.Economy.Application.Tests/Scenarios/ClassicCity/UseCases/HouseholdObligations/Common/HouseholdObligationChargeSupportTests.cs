using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common
{
    public sealed class HouseholdObligationChargeSupportTests
    {
        [Fact]
        public async Task TryChargeAsync_ReturnsNotDueWhenFrozenClockIsBeforeDueDate()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant Household",
                openingBalance: 300m);
            CityBusiness providerBusiness = CreateBusiness(
                cityId: cityId,
                name: "Landlord",
                kind: CityBusinessKind.Landlord,
                initialCapital: 500m);
            CityHouseholdObligation obligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: "Monthly Rent",
                kind: CityHouseholdObligationKind.Rent,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 80m,
                taxAmount: 8m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var support = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                timeProvider: timeProvider);

            HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
                obligation: obligation,
                description: "pre-due check",
                occurredAtUtc: null,
                cancellationToken: CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(
                expected: "NotDue",
                actual: result.FailureCode);
            Assert.Empty(householdLedgerRepository.AddedEntries);
            Assert.Empty(businessLedgerRepository.AddedEntries);
            Assert.Equal(
                expected: 300m,
                actual: householdAccount.Balance.Amount);
            Assert.Equal(
                expected: 500m,
                actual: providerBusiness.Balance.Amount);
        }

        [Fact]
        public async Task TryChargeAsync_ChargesDueObligationWithFrozenTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant Household",
                openingBalance: 300m);
            CityBusiness providerBusiness = CreateBusiness(
                cityId: cityId,
                name: "Landlord",
                kind: CityBusinessKind.Landlord,
                initialCapital: 500m);
            CityHouseholdObligation obligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: "Monthly Rent",
                kind: CityHouseholdObligationKind.Rent,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 80m,
                taxAmount: 8m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var support = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                timeProvider: timeProvider);

            HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
                obligation: obligation,
                description: "Rent collection",
                occurredAtUtc: null,
                cancellationToken: CancellationToken.None);

            CityHouseholdAccountLedgerEntry householdEntry = Assert.Single(householdLedgerRepository.AddedEntries);
            CityBusinessLedgerEntry businessEntry = Assert.Single(businessLedgerRepository.AddedEntries);
            Assert.True(result.Succeeded);
            Assert.Equal(
                expected: 80m,
                actual: result.ChargedAmount.Amount);
            Assert.Equal(
                expected: 8m,
                actual: result.ChargedTaxAmount.Amount);
            Assert.Equal(
                expected: 1,
                actual: result.SettledInstallmentCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: householdEntry.OccurredAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: businessEntry.OccurredAtUtc);
            Assert.Equal(
                expected: 220m,
                actual: householdAccount.Balance.Amount);
            Assert.Equal(
                expected: 580m,
                actual: providerBusiness.Balance.Amount);
            Assert.Equal(
                expected: 8m,
                actual: providerBusiness.TaxReserve.Amount);
            Assert.Equal(
                expected: 72m,
                actual: providerBusiness.TotalNetSalesRevenue.Amount);
            Assert.Equal(
                expected: 1,
                actual: obligation.ChargeCount);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 7,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: obligation.NextChargeDueAtUtc);
            Assert.Equal(
                expected: obligation.Id.ToString("N"),
                actual: result.LedgerEntry!.ReferenceCode);
        }

        [Fact]
        public async Task TryChargeAsync_MarksMissedChargeWhenBalanceIsInsufficient()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount householdAccount = CreateHouseholdAccount(
                cityId: cityId,
                name: "Tenant Household",
                openingBalance: 20m);
            CityBusiness providerBusiness = CreateBusiness(
                cityId: cityId,
                name: "Utility Provider",
                kind: CityBusinessKind.Utility,
                initialCapital: 500m);
            CityHouseholdObligation obligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdAccount.Id,
                providerBusinessId: providerBusiness.Id,
                name: "Power",
                kind: CityHouseholdObligationKind.Utilities,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 80m,
                taxAmount: 8m);
            var householdAccountRepository = new FakeCityHouseholdAccountRepository
            {
                Accounts = [householdAccount]
            };
            var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
            var businessRepository = new FakeCityBusinessRepository
            {
                Businesses = [providerBusiness]
            };
            var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 10,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var support = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                timeProvider: timeProvider);

            HouseholdObligationChargeAttemptResult result = await support.TryChargeAsync(
                obligation: obligation,
                description: null,
                occurredAtUtc: null,
                cancellationToken: CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal(
                expected: "InsufficientBalance",
                actual: result.FailureCode);
            Assert.Equal(
                expected: 1,
                actual: obligation.MissedChargeCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: obligation.LastChargeAttemptedAtUtc);
            Assert.Empty(householdLedgerRepository.AddedEntries);
            Assert.Empty(businessLedgerRepository.AddedEntries);
            Assert.Equal(
                expected: 20m,
                actual: householdAccount.Balance.Amount);
            Assert.Equal(
                expected: 500m,
                actual: providerBusiness.Balance.Amount);
        }
    }
}
