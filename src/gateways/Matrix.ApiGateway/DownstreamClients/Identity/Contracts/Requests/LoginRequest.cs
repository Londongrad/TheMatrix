namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests
{
    public sealed class LoginRequest
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string DeviceId { get; set; } = null!;
        public string DeviceName { get; set; } = null!;
    }
}
