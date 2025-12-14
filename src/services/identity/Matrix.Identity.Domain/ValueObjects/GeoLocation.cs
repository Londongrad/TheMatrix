namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record GeoLocation
    {
        private GeoLocation() { }

        private GeoLocation(
            string country,
            string? region,
            string? city)
        {
            Country = country;
            Region = region;
            City = city;
        }

        public string Country { get; } = null!;
        public string? Region { get; }
        public string? City { get; }

        public static GeoLocation Create(
            string country,
            string? region,
            string? city)
        {
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException(
                    message: "Country cannot be null or whitespace.",
                    paramName: nameof(country));

            string? normalizedRegion = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
            string? normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

            return new GeoLocation(
                country: country.Trim(),
                region: normalizedRegion,
                city: normalizedCity);
        }
    }
}
