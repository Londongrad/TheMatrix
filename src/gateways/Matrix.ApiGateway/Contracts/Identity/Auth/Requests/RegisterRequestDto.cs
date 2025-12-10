using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public class RegisterRequestDto
    {
        [Required] [EmailAddress] public string Email { get; set; } = null!;

        [Required]
        [MinLength(3)]
        [MaxLength(16)]
        public string Username { get; set; } = null!;

        [MinLength(6)] public required string Password { get; set; }

        [Required] [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;
    }
}
