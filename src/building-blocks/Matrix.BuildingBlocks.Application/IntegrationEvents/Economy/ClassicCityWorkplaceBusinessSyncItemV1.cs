namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Economy
{
    public sealed record ClassicCityWorkplaceBusinessSyncItemV1(
        Guid WorkplaceId,
        string ExternalReferenceCode,
        string Name,
        string JobTitle,
        int ActiveWorkerCount);
}
