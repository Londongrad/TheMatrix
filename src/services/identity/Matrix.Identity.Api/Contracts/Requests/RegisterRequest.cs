namespace Matrix.Identity.Api.Contracts.Requests
{
    public sealed class RegisterRequest
    {
        public required string Email { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
