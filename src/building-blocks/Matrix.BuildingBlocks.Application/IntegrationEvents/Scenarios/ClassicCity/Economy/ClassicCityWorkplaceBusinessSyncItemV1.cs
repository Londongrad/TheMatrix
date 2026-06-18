namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityWorkplaceBusinessSyncItemV1(
        Guid WorkplaceId,
        string ExternalReferenceCode,
        string Name,
        string JobTitle,
        int ActiveWorkerCount);
}
