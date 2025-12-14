namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests
{
    public sealed class LoginRequest
    {
        public required string Login { get; set; }
        public required string Password { get; set; }

        public required string DeviceId { get; set; }
        public required string DeviceName { get; set; }

        public bool RememberMe { get; set; } = true;
    }
}
