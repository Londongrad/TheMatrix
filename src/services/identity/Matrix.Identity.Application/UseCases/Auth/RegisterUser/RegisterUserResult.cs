namespace Matrix.Identity.Application.UseCases.Auth.RegisterUser
{
    public sealed class RegisterUserResult
    {
        public Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string Username { get; init; }
    }
}
