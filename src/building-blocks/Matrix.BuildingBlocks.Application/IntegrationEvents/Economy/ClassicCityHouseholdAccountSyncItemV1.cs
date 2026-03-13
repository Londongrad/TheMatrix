namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record ClassicCityHouseholdAccountSyncItemV1(
        Guid HouseholdId,
        string ExternalReferenceCode,
        string Name,
        int MemberCount,
        decimal OpeningBalanceAmount,
        bool IsHoused,
        DateTimeOffset CreatedAtUtc);
}
