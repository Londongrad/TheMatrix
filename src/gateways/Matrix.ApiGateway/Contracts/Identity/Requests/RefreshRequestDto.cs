namespace Matrix.ApiGateway.Contracts.Identity.Requests
{
    public sealed class RefreshRequestDto
    {
        public required string DeviceId { get; init; }
    }
}
