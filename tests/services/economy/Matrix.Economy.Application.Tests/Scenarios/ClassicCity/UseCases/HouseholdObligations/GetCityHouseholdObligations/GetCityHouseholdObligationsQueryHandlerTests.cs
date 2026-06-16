using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetCityHouseholdObligations
{
    public sealed class GetCityHouseholdObligationsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsObligationsToDtos()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdObligation obligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                providerBusinessId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                name: "Monthly Rent",
                kind: CityHouseholdObligationKind.Rent,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 80m,
                taxAmount: 8m);
            obligation.MarkCharged(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var repository = new FakeCityHouseholdObligationRepository
            {
                Obligations =
                [
                    obligation,
                    CreateHouseholdObligation(
                        cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        householdAccountId: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                        providerBusinessId: Guid.Parse("40000000-0000-0000-0000-000000000001"),
                        name: "Utilities",
                        kind: CityHouseholdObligationKind.Utilities,
                        cadence: CityHouseholdObligationBillingCadence.Monthly,
                        chargeAmount: 40m,
                        taxAmount: 4m)
                ]
            };
            var handler = new GetCityHouseholdObligationsQueryHandler(repository);

            IReadOnlyList<CityHouseholdObligationDto> result =
                await handler.Handle(
                    request: new GetCityHouseholdObligationsQuery(cityId),
                    cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: repository.RequestedCityId);
            Assert.Single(result);
            Assert.Equal(
                expected: "Monthly Rent",
                actual: result[0].Name);
            Assert.Equal(
                expected: "Rent",
                actual: result[0].Kind);
            Assert.True(result[0].IsActive);
            Assert.Equal(
                expected: 80m,
                actual: result[0].ChargeAmount);
            Assert.Equal(
                expected: 8m,
                actual: result[0].TaxAmount);
            Assert.Equal(
                expected: 1,
                actual: result[0].ChargeCount);
            Assert.NotNull(result[0].LastChargedAtUtc);
        }
    }
}
