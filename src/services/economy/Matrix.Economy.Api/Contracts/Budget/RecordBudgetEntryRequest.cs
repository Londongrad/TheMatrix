namespace Matrix.Economy.Api.Contracts.Budget
{
    public sealed record RecordBudgetEntryRequest(
        string Category,
        decimal Amount,
        string Title,
        string? Description);
}
