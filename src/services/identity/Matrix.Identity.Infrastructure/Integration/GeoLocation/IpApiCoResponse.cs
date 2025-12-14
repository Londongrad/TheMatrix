using System.Text.Json.Serialization;

namespace Matrix.Identity.Infrastructure.Integration.GeoLocation
{
    /// <summary>
    ///     Minimal response model for ipapi.co JSON.
    ///     We only care about country and city.
    /// </summary>
    internal sealed class IpApiCoResponse
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("country_name")]
        public string? CountryName { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("error")]
        public bool Error { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
