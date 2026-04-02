using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.Auth
{
    public class RefreshTokenPayload
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
