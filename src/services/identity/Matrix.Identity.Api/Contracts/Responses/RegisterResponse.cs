namespace Matrix.Identity.Api.Contracts.Responses
{
    public sealed class RegisterResponse
    {
        public Guid UserId { get; set; }

        public required string Email { get; set; }

        public required string Username { get; set; }
    }
}