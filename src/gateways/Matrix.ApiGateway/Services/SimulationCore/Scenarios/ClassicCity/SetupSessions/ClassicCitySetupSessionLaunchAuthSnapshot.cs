namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionLaunchAuthSnapshot
    {
        public Guid UserId { get; init; }
        public string? Jti { get; init; }
        public int PermissionsVersion { get; init; }
        public string[] EffectivePermissions { get; init; } = [];
        public DateTimeOffset CapturedAtUtc { get; init; }
    }
}
