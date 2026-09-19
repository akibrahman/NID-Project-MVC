using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NID_Project.Models
{
    public class UserBallot
    {
        public int Id { get; set; }

        [Required]
        public int VotingId { get; set; }
        [ForeignKey(nameof(VotingId))]
        public Voting? Voting { get; set; }

        [Required]
        public int NomineeId { get; set; }
        [ForeignKey(nameof(NomineeId))]
        public Nominee? Nominee { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}