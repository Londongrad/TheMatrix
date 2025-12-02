namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record GeoLocation
    {
        public string Country { get; } = null!;
        public string? Region { get; }
        public string? City { get; }

        private GeoLocation() { }

        private GeoLocation(string country, string? region, string? city)
        {
            Country = country;
            Region = region;
            City = city;
        }

        public static GeoLocation Create(string country, string? region, string? city)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                throw new ArgumentException("Country cannot be null or whitespace.", nameof(country));
            }

            var normalizedRegion = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
            var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

            return new GeoLocation(country.Trim(), normalizedRegion, normalizedCity);
        }
    }
}
