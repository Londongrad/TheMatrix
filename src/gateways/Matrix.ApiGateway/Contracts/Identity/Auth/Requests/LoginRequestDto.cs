using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class LoginRequestDto
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public required string Login { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 6)]
        public required string Password { get; set; }

        [Required]
        [StringLength(64)]
        public required string DeviceId { get; set; }

        [Required]
        [StringLength(128)]
        public required string DeviceName { get; set; }

        public bool RememberMe { get; set; } = true;
    }
}
