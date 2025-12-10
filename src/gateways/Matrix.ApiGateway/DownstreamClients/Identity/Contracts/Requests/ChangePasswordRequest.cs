namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests
{
    public sealed class ChangePasswordRequest
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
