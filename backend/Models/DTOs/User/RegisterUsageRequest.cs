using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class RegisterUsageRequest
    {
        [Required]
        public string MetricType { get; set; } = string.Empty;
    }
}
