using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NID_Project.Models
{
    public enum VotingStatus
    {
        Opening,   // admin is setting up; users can't vote yet
        Running,   // users can vote; results hidden
        Closed     // results revealed to all
    }

    public class Voting
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Area")]
        public string Area { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Position")]
        public string Position { get; set; } = string.Empty;

        public string? Description { get; set; }

        public VotingStatus Status { get; set; } = VotingStatus.Opening;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public ICollection<Nominee> Nominees { get; set; } = new List<Nominee>();
        public ICollection<UserBallot> Ballots { get; set; } = new List<UserBallot>();
    }
}