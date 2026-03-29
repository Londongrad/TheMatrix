namespace Matrix.Economy.Contracts.Budget.Requests
{
    public sealed record AuthorizeBudgetOperationRequest(
        string Category,
        string OperationKind,
        string RequestedIntensity,
        decimal EstimatedAmount,
        bool EmergencyOverride = false);
}
