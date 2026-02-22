namespace Matrix.Identity.Contracts.Self.Account.Responses
{
    public sealed class SecurityActivityItemResponse
    {
        public required string EventType { get; init; }
        public bool IsSuccessful { get; init; }
        public DateTime OccurredAtUtc { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? DeviceId { get; init; }
        public string? DeviceName { get; init; }
        public string? Details { get; init; }
    }
}
