using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class UpdateAccountPlanRequest
    {
        [Required]
        public string PlanId { get; set; } = string.Empty;
    }
}
