namespace Matrix.Identity.Api.Contracts.Responses
{
    public class UserProfileResponse
    {
        public required Guid UserId { get; set; }

        public required string Email { get; set; }

        public required string Username { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
