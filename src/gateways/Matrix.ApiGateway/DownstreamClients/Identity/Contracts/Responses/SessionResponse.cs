namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Responses
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

        // Просто пробрасываем то, что посчитал Identity
        public string? Location { get; set; }
    }
}
