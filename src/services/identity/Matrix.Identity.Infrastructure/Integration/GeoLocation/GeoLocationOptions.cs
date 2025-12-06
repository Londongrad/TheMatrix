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
        public string EndpointTemplate { get; init; } = null!;

        /// <summary>
        ///     Whether geo location lookup is enabled.
        ///     If disabled, the service will always return null.
        /// </summary>
        public bool Enabled { get; init; }

        /// <summary>
        ///     Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds { get; init; }
    }
}
