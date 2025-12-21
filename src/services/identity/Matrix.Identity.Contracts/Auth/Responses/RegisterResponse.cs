namespace Matrix.Identity.Contracts.Auth.Responses
{
    public sealed class RegisterResponse
    {
        public Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string Username { get; init; }
    }
}
