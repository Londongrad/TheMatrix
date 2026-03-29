namespace Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation
{
    public sealed record CityBudgetOperationAuthorizationDto(
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
