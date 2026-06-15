using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetHouseholdObligations;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdObligations.GetHouseholdObligations
{
    public sealed class GetHouseholdObligationsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsObligationsForRequestedHousehold()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var householdAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001");
            CityHouseholdObligation dueObligation = CreateHouseholdObligation(
                cityId: cityId,
                householdAccountId: householdAccountId,
                providerBusinessId: Guid.Parse("50000000-0000-0000-0000-000000000001"),
                name: "Utilities",
                kind: CityHouseholdObligationKind.Utilities,
                cadence: CityHouseholdObligationBillingCadence.Monthly,
                chargeAmount: 60m,
                taxAmount: 6m);
            dueObligation.MarkCharged(
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
                    dueObligation,
                    CreateHouseholdObligation(
                        cityId: cityId,
                        householdAccountId: Guid.Parse("40000000-0000-0000-0000-000000000002"),
                        providerBusinessId: Guid.Parse("50000000-0000-0000-0000-000000000002"),
                        name: "Rent",
                        kind: CityHouseholdObligationKind.Rent,
                        cadence: CityHouseholdObligationBillingCadence.Monthly,
                        chargeAmount: 140m,
                        taxAmount: 14m)
                ]
            };
            var handler = new GetHouseholdObligationsQueryHandler(repository);

            IReadOnlyList<CityHouseholdObligationDto> result = await handler.Handle(
                request: new GetHouseholdObligationsQuery(householdAccountId),
                cancellationToken: CancellationToken.None);

            CityHouseholdObligationDto dto = Assert.Single(result);
            Assert.Equal(
                expected: householdAccountId,
                actual: repository.RequestedHouseholdAccountId);
            Assert.Equal(
                expected: "Utilities",
                actual: dto.Name);
            Assert.Equal(
                expected: "Utilities",
                actual: dto.Kind);
            Assert.Equal(
                expected: 60m,
                actual: dto.ChargeAmount);
            Assert.Equal(
                expected: 6m,
                actual: dto.TaxAmount);
            Assert.Equal(
                expected: 1,
                actual: dto.ChargeCount);
            Assert.NotNull(dto.LastChargedAtUtc);
        }
    }
}
