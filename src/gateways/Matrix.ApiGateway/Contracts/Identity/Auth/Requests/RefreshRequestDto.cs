using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class RefreshRequestDto
    {
        [Required]
        [StringLength(64)]
        public required string DeviceId { get; set; }
    }
}
