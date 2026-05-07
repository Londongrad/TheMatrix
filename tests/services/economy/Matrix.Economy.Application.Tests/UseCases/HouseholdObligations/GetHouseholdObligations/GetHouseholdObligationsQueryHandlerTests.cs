using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.HouseholdObligations;
using Matrix.Economy.Application.UseCases.HouseholdObligations.GetHouseholdObligations;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdObligations.GetHouseholdObligations;

public sealed class GetHouseholdObligationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsObligationsForRequestedHousehold()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Guid householdAccountId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var dueObligation = CreateHouseholdObligation(
            cityId: cityId,
            householdAccountId: householdAccountId,
            providerBusinessId: Guid.Parse("50000000-0000-0000-0000-000000000001"),
            name: "Utilities",
            kind: CityHouseholdObligationKind.Utilities,
            cadence: CityHouseholdObligationBillingCadence.Monthly,
            chargeAmount: 60m,
            taxAmount: 6m);
        dueObligation.MarkCharged(new DateTimeOffset(2048, 5, 7, 9, 0, 0, TimeSpan.Zero));
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
            new GetHouseholdObligationsQuery(householdAccountId),
            CancellationToken.None);

        CityHouseholdObligationDto dto = Assert.Single(result);
        Assert.Equal(householdAccountId, repository.RequestedHouseholdAccountId);
        Assert.Equal("Utilities", dto.Name);
        Assert.Equal("Utilities", dto.Kind);
        Assert.Equal(60m, dto.ChargeAmount);
        Assert.Equal(6m, dto.TaxAmount);
        Assert.Equal(1, dto.ChargeCount);
        Assert.NotNull(dto.LastChargedAtUtc);
    }
}
