using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public class RegisterRequestDto
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }

        [Compare(nameof(Password))]
        public required string ConfirmPassword { get; set; }
    }
}
