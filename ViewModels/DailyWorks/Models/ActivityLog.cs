using System;

namespace ProGlassAutomation.ViewModels.DailyWorks.Models
{
    public class ActivityLog
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public string User { get; set; } = "";
    }
}
