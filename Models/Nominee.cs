using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NID_Project.Models
{
    public class Nominee
    {
        public int Id { get; set; }

        [Required]
        public int VotingId { get; set; }
        [ForeignKey(nameof(VotingId))]
        public Voting? Voting { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? TeamName { get; set; }

        public string? PhotoPath { get; set; }

        // "marka" — a text symbol, not an image
        [Required]
        public string Sign { get; set; } = string.Empty;

        public ICollection<UserBallot> Ballots { get; set; } = new List<UserBallot>();
    }
}