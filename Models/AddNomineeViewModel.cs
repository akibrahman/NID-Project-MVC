using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace NID_Project.Models
{
    public class AddNomineeViewModel
    {
        public int VotingId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? TeamName { get; set; }

        [Required]
        [Display(Name = "Sign (marka)")]
        public string Sign { get; set; } = string.Empty;

        [Display(Name = "Photo")]
        public IFormFile? Photo { get; set; }
    }
}