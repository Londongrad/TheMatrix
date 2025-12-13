namespace Matrix.ApiGateway.Contracts.Identity.Account
{
    public class UserProfileResponseDto
    {
        public required Guid UserId { get; set; }

        public required string Email { get; set; }

        public required string Username { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
