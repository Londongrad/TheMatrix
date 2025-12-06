namespace Matrix.Identity.Api.Contracts.Responses
{
    public sealed class SessionResponse
    {
        public Guid Id { get; set; }

        public string DeviceId { get; set; } = null!;
        public string DeviceName { get; set; } = null!;
        public string UserAgent { get; set; } = null!;
        public string? IpAddress { get; set; }

        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastUsedAtUtc { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        ///     Convenience-строка для фронта: "City, Region, Country" / null
        /// </summary>
        public string? Location
        {
            get
            {
                var parts = new List<string>(3);

                if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);

                if (!string.IsNullOrWhiteSpace(Region)) parts.Add(Region);

                if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country);

                return parts.Count == 0 ? null : string.Join(separator: ", ", values: parts);
            }
        }
    }
}
