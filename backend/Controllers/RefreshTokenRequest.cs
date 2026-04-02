using System;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Controllers
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
