namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities
{
    public static class PopulationBootstrapFailureCodes
    {
        public const string PopulationValidationFailed = "POPULATION_VALIDATION_FAILED";
        public const string PopulationConflict = "POPULATION_CONFLICT";
        public const string PopulationDependencyNotFound = "POPULATION_DEPENDENCY_NOT_FOUND";
        public const string PopulationResponseInvalid = "POPULATION_RESPONSE_INVALID";
        public const string PopulationServiceUnavailable = "POPULATION_SERVICE_UNAVAILABLE";
        public const string PopulationTimeout = "POPULATION_TIMEOUT";
        public const string PopulationTransportError = "POPULATION_TRANSPORT_ERROR";
        public const string PopulationUnexpectedError = "POPULATION_UNEXPECTED_ERROR";
    }
}
