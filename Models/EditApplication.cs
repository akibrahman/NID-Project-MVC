using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NID_Project.Models
{
    public enum ApplicationStatus
    {
        Pending,        // created, waiting for moderator to review the request
        Approved,       // moderator approved the request, user can now edit
        Rejected,       // moderator rejected the request (phase 1)
        Edited,         // user submitted the edits, waiting for moderator to review changes
        Completed,      // moderator approved the edits, changes saved to user
        ChangeRejected  // moderator rejected the edits (phase 2)
    }

    public class EditApplication
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // Comma-separated field names the user wants to change
        [Required]
        public string RequestedFields { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        // ----- Phase 1: moderator reviews the application -----
        public string? ReviewedByModeratorId { get; set; }
        [ForeignKey(nameof(ReviewedByModeratorId))]
        public ApplicationUser? ReviewedByModerator { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewMessage { get; set; }

        // ----- Phase 2: user submits edits -----
        public DateTime? EditedAt { get; set; }
        public string? EditedDataJson { get; set; }   // JSON of { fieldName: newValue }

        // ----- Phase 2: moderator reviews the edits -----
        public string? ChangeReviewedByModeratorId { get; set; }
        [ForeignKey(nameof(ChangeReviewedByModeratorId))]
        public ApplicationUser? ChangeReviewedByModerator { get; set; }
        public DateTime? ChangeReviewedAt { get; set; }
        public string? ChangeReviewMessage { get; set; }
    }
}