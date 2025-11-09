namespace Matrix.Economy.Application.Messages
{
    /// <summary>
    /// Сообщение, которое приходит в Economy от брокера,
    /// основано на integration event из Population.
    /// </summary>
    public sealed record MonthlyIncomeEarnedMessage(
        Guid PersonId,
        Guid HouseholdId,
        Guid DistrictId,
        Guid WorkplaceId,
        decimal GrossAmount,
        decimal NetAmount,
        decimal TaxAmount,
        int SimulationMonth,
        string CorrelationId);
}
