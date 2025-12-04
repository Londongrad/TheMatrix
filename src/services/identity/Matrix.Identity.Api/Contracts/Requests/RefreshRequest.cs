namespace Matrix.Identity.Api.Contracts.Requests
{
    public sealed class RefreshRequest
    {
        public required string RefreshToken { get; set; }
        public required string DeviceId { get; set; }
    }
}