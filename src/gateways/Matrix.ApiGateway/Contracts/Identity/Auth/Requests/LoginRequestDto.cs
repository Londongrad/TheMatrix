using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class LoginRequestDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string DeviceId { get; set; }

        [Required]
        public required string DeviceName { get; set; }

        public bool RememberMe { get; set; } = true;
    }
}
