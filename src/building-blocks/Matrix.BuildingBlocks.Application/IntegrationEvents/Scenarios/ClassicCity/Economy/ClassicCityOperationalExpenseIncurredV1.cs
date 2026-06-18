namespace Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy
{
    public sealed record ClassicCityOperationalExpenseIncurredV1(
        Guid ExpenseId,
        Guid CityId,
        string Category,
        decimal Amount,
        string Title,
        string? Description,
        string SourceService,
        string OperationKind,
        DateTimeOffset OccurredAtUtc);
}
