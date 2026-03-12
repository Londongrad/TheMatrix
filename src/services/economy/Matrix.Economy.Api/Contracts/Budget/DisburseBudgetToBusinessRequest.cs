namespace Matrix.Economy.Api.Contracts.Budget
{
    public sealed record DisburseBudgetToBusinessRequest(
        Guid BusinessId,
        string Category,
        decimal Amount,
        string Title,
        string? Description);
}
