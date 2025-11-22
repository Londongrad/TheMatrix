namespace Matrix.Identity.Api.Contracts
{
    public sealed class MeResponse
    {
        public required Guid UserId { get; set; }

        public required string Email { get; set; }
        
        public required string Username { get; set; }
    }
}