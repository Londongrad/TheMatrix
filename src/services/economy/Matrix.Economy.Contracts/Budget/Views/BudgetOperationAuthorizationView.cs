namespace Matrix.Economy.Contracts.Budget.Views
{
    public sealed record BudgetOperationAuthorizationView(
        Guid CityId,
        string Category,
        string OperationKind,
        string RequestedIntensity,
        string? ApprovedIntensity,
        string Status,
        string AuthorizationLevel,
        decimal AvailableAmount,
        decimal EstimatedAmount,
        decimal PressureIndex,
        bool EmergencyOverrideRequested,
        bool AuthorizedByEmergencyOverride,
        string Summary);
}
