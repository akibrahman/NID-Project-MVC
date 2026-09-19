using System.Collections.Generic;

namespace NID_Project.Models
{
    public class AdminDashboardViewModel
    {
        // Existing
        public int TotalModerators { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPendingUsers { get; set; }

        // New
        public int TotalApplications { get; set; }

        // Pie chart data: status name -> count
        public Dictionary<string, int> ApplicationsByStatus { get; set; } = new();

        // Bar chart data: moderator name -> number of reviews
        public Dictionary<string, int> ModeratorActivity { get; set; } = new();
    }
}