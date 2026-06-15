using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.IssueHouseholdObligationCharge;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdObligations.IssueHouseholdObligationCharge
{
    public sealed class IssueHouseholdObligationChargeCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ChargesObligationAndSavesChanges()
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
            var obligationRepository = new FakeCityHouseholdObligationRepository
            {
                Obligations = [obligation]
            };
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
            var chargeSupport = new HouseholdObligationChargeSupport(
                householdAccountRepository: householdAccountRepository,
                householdLedgerRepository: householdLedgerRepository,
                businessRepository: businessRepository,
                businessLedgerRepository: businessLedgerRepository,
                timeProvider: timeProvider);
            var unitOfWork = new FakeEconomyUnitOfWork();
            var handler = new IssueHouseholdObligationChargeCommandHandler(
                obligationRepository: obligationRepository,
                chargeSupport: chargeSupport,
                unitOfWork: unitOfWork);
            var command = new IssueHouseholdObligationChargeCommand(
                ObligationId: obligation.Id,
                Description: "Scheduled charge");

            CityHouseholdAccountLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: "ObligationCharge",
                actual: result.Kind);
            Assert.Equal(
                expected: "Obligation",
                actual: result.Source);
            Assert.Equal(
                expected: 80m,
                actual: result.Amount);
            Assert.Equal(
                expected: obligation.Id.ToString("N"),
                actual: result.ReferenceCode);
            Assert.Single(householdLedgerRepository.AddedEntries);
            Assert.Single(businessLedgerRepository.AddedEntries);
        }

        [Fact]
        public async Task Handle_ThrowsWhenObligationIsMissing()
        {
            var obligationRepository = new FakeCityHouseholdObligationRepository();
            var chargeSupport = new HouseholdObligationChargeSupport(
                householdAccountRepository: new FakeCityHouseholdAccountRepository(),
                householdLedgerRepository: new FakeCityHouseholdAccountLedgerRepository(),
                businessRepository: new FakeCityBusinessRepository(),
                businessLedgerRepository: new FakeCityBusinessLedgerRepository(),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 7,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            var unitOfWork = new FakeEconomyUnitOfWork();
            var handler = new IssueHouseholdObligationChargeCommandHandler(
                obligationRepository: obligationRepository,
                chargeSupport: chargeSupport,
                unitOfWork: unitOfWork);
            var obligationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => handler.Handle(
                    request: new IssueHouseholdObligationChargeCommand(
                        ObligationId: obligationId,
                        Description: null),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: $"Obligation '{obligationId}' was not found.",
                actual: exception.Message);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
        }
    }
}
