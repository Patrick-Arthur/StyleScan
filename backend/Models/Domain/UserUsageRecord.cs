using System;

namespace StyleScan.Backend.Models.Domain
{
    public class UserUsageRecord
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public string PeriodKey { get; set; } = string.Empty;
        public int Used { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
    }
}
