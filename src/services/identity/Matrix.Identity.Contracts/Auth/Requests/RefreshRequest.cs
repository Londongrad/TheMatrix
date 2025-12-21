namespace Matrix.Identity.Contracts.Auth.Requests
{
    public sealed class RefreshRequest
    {
        public required string RefreshToken { get; init; }
        public required string DeviceId { get; init; }
    }
}
