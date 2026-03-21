namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    internal static class ClassicCitySetupSessionFailureCodes
    {
        public const string InvalidLaunchState = "Gateway.ClassicCitySetup.InvalidLaunchState";
        public const string SessionBusy = "Gateway.ClassicCitySetup.SessionBusy";
        public const string SessionLockUnavailable = "Gateway.ClassicCitySetup.SessionLockUnavailable";
        public const string LaunchAuthContextUnavailable = "Gateway.ClassicCitySetup.LaunchAuthContextUnavailable";
        public const string LaunchRequestMissing = "Gateway.ClassicCitySetup.LaunchRequestMissing";
        public const string LaunchQueueUnavailable = "Gateway.ClassicCitySetup.LaunchQueueUnavailable";
        public const string CityCreateValidationFailed = "Gateway.ClassicCitySetup.CityCreateValidationFailed";
        public const string CityCreateConflict = "Gateway.ClassicCitySetup.CityCreateConflict";
        public const string CityCreateTransportError = "Gateway.ClassicCitySetup.CityCreateTransportError";
        public const string CityCreateUnexpectedError = "Gateway.ClassicCitySetup.CityCreateUnexpectedError";
        public const string ReconciliationCityNotFound = "Gateway.ClassicCitySetup.ReconciliationCityNotFound";
        public const string ProvisioningUnexpectedError = "Gateway.ClassicCitySetup.ProvisioningUnexpectedError";
    }
}
