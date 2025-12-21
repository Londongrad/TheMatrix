namespace Matrix.Identity.Contracts.Sessions.Responses
{
    public sealed class SessionResponse
    {
        public Guid Id { get; init; }

        public string DeviceId { get; init; } = null!;
        public string DeviceName { get; init; } = null!;
        public string UserAgent { get; init; } = null!;
        public string? IpAddress { get; init; }

        public string? Country { get; init; }
        public string? Region { get; init; }
        public string? City { get; init; }

        public DateTime CreatedAtUtc { get; init; }
        public DateTime? LastUsedAtUtc { get; init; }

        public bool IsActive { get; init; }

        /// <summary>
        ///     Convenience-строка для фронта: "City, Region, Country" / null
        /// </summary>
        public string? Location
        {
            get
            {
                var parts = new List<string>(3);

                if (!string.IsNullOrWhiteSpace(City))
                    parts.Add(City);

                if (!string.IsNullOrWhiteSpace(Region))
                    parts.Add(Region);

                if (!string.IsNullOrWhiteSpace(Country))
                    parts.Add(Country);

                return parts.Count == 0
                    ? null
                    : string.Join(
                        separator: ", ",
                        values: parts);
            }
        }
    }
}
