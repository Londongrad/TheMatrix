using System.ComponentModel.DataAnnotations;

namespace Matrix.ApiGateway.Contracts.Identity.Auth.Requests
{
    public sealed class RefreshRequestDto
    {
        [Required]
        public required string DeviceId { get; set; }
    }
}
