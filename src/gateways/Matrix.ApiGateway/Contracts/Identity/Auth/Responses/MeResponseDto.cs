namespace Matrix.ApiGateway.Contracts.Identity.Auth.Responses
{
    public sealed class MeResponseDto
    {
        public Guid UserId { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
