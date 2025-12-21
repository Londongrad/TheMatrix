namespace Matrix.Identity.Contracts.Auth.Requests
{
    public sealed class RegisterRequest
    {
        public required string Email { get; init; }
        public required string Username { get; init; }
        public required string Password { get; init; }
    }
}
