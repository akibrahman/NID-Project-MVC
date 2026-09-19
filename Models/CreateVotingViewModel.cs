using System.ComponentModel.DataAnnotations;

namespace NID_Project.Models
{
    public class CreateVotingViewModel
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        [Required]
        public string Position { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}