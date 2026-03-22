namespace Matrix.Economy.Contracts.Budget.Requests
{
    public sealed record DisburseBudgetToBusinessRequest(
        Guid BusinessId,
        string Category,
        decimal Amount,
        string Title,
        string? Description);
}
