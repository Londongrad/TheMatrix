namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class LoginRequestDto
    {
        public required string Login { get; set; }
        public required string Password { get; set; }

        public required string DeviceId { get; set; }
        public required string DeviceName { get; set; }
    }
}
