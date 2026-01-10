namespace Matrix.Identity.Infrastructure.Integration.GeoLocation
{
    /// <summary>
    ///     Configuration for GeoLocationService.
    /// </summary>
    public sealed class GeoLocationOptions
    {
        public const string SectionName = "GeoLocation";

        /// <summary>
        ///     Endpoint template, must contain "{ip}" placeholder.
        ///     Example: "https://ipapi.co/{ip}/json/".
        /// </summary>
        public string EndpointTemplate { get; init; } = "https://ipapi.co/{ip}/json/";

        /// <summary>
        ///     Whether geolocation lookup is enabled.
        ///     If disabled, the service will always return null.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        ///     Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; init; } = 10;
    }
}
