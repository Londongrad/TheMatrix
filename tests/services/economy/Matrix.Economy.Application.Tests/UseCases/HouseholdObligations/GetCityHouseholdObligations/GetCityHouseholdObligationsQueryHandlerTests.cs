using Matrix.Economy.Application.UseCases.HouseholdObligations.GetCityHouseholdObligations;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.GetCityHouseholdObligations;

public sealed class GetCityHouseholdObligationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsObligationsToDtos()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var obligation = CreateHouseholdObligation(
            cityId: cityId,
            householdAccountId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            providerBusinessId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
            name: "Monthly Rent",
            kind: CityHouseholdObligationKind.Rent,
            cadence: CityHouseholdObligationBillingCadence.Monthly,
            chargeAmount: 80m,
            taxAmount: 8m);
        obligation.MarkCharged(new DateTimeOffset(2048, 5, 7, 9, 0, 0, TimeSpan.Zero));
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

        IReadOnlyList<Matrix.Economy.Application.UseCases.HouseholdObligations.CityHouseholdObligationDto> result =
            await handler.Handle(new GetCityHouseholdObligationsQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, repository.RequestedCityId);
        Assert.Single(result);
        Assert.Equal("Monthly Rent", result[0].Name);
        Assert.Equal("Rent", result[0].Kind);
        Assert.True(result[0].IsActive);
        Assert.Equal(80m, result[0].ChargeAmount);
        Assert.Equal(8m, result[0].TaxAmount);
        Assert.Equal(1, result[0].ChargeCount);
        Assert.NotNull(result[0].LastChargedAtUtc);
    }
}
