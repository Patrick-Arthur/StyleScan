namespace StyleScan.Backend.Models.DTOs.User
{
    public class AccountPlanUsageResponse
    {
        public string MetricType { get; set; } = string.Empty;
        public string PeriodKey { get; set; } = string.Empty;
        public int Used { get; set; }
    }
}
