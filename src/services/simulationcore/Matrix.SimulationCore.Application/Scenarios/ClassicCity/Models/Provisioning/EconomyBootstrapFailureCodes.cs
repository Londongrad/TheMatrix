namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning
{
    public static class EconomyBootstrapFailureCodes
    {
        public const string EconomyValidationFailed = "ECONOMY_VALIDATION_FAILED";
        public const string EconomyConflict = "ECONOMY_CONFLICT";
        public const string EconomyDependencyNotFound = "ECONOMY_DEPENDENCY_NOT_FOUND";
        public const string EconomyResponseInvalid = "ECONOMY_RESPONSE_INVALID";
        public const string EconomyServiceUnavailable = "ECONOMY_SERVICE_UNAVAILABLE";
        public const string EconomyTimeout = "ECONOMY_TIMEOUT";
        public const string EconomyTransportError = "ECONOMY_TRANSPORT_ERROR";
        public const string EconomyUnexpectedError = "ECONOMY_UNEXPECTED_ERROR";
    }
}
