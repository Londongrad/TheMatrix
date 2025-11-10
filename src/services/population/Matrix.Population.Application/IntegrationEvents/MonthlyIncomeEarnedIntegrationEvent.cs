namespace Matrix.Population.Application.IntegrationEvents
{
    public sealed record MonthlyIncomeEarnedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        string CorrelationId,

        Guid PersonId,
        Guid HouseholdId,
        Guid DistrictId,
        Guid WorkplaceId,

        decimal GrossAmount,
        decimal NetAmount,
        decimal TaxAmount,

        int SimulationMonth);
}
