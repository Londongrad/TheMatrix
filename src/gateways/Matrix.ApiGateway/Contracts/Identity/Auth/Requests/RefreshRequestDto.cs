namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class RefreshRequestDto
    {
        public required string DeviceId { get; set; }
    }
}
