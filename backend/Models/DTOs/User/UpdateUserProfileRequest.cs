using System;
using System.ComponentModel.DataAnnotations;

namespace StyleScan.Backend.Models.DTOs.User
{
    public class UpdateUserProfileRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateOnly DateOfBirth { get; set; }

        public string? Gender { get; set; }
    }
}
