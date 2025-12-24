namespace Matrix.Identity.Contracts.Self.Auth.Requests
{
    public sealed class LoginRequest
    {
        public required string Login { get; init; }

        public required string Password { get; init; }

        public required string DeviceId { get; init; }

        public required string DeviceName { get; init; }

        public bool RememberMe { get; init; } = true;
    }
}
