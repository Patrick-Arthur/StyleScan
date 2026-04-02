using System;
using System.Collections.Generic;
using System.Globalization;
using StyleScan.Backend.Models.Domain;
using StyleScan.Backend.Models.DTOs.User;

namespace StyleScan.Backend.Services.Support
{
    public sealed class AccountPlanDefinition
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public decimal MonthlyPrice { get; init; }
        public int Avatars { get; init; }
        public int AvatarTryOnsPerWeek { get; init; }
        public int RealisticRendersPerMonth { get; init; }
        public int SavedLooks { get; init; }
    }

    public static class AccountPlanCatalog
    {
        private static readonly Dictionary<string, AccountPlanDefinition> Plans = new(StringComparer.OrdinalIgnoreCase)
        {
            [AccountPlanType.Free] = new()
            {
                Id = AccountPlanType.Free,
                DisplayName = "Free",
                MonthlyPrice = 0m,
                Avatars = 1,
                AvatarTryOnsPerWeek = 6,
                RealisticRendersPerMonth = 2,
                SavedLooks = 5
            },
            [AccountPlanType.Plus] = new()
            {
                Id = AccountPlanType.Plus,
                DisplayName = "Style Plus",
                MonthlyPrice = 1.00m,
                Avatars = 3,
                AvatarTryOnsPerWeek = 24,
                RealisticRendersPerMonth = 12,
                SavedLooks = 30
            },
            [AccountPlanType.Pro] = new()
            {
                Id = AccountPlanType.Pro,
                DisplayName = "Style Pro",
                MonthlyPrice = 39.90m,
                Avatars = 8,
                AvatarTryOnsPerWeek = 60,
                RealisticRendersPerMonth = 30,
                SavedLooks = 9999
            },
            [AccountPlanType.Atelier] = new()
            {
                Id = AccountPlanType.Atelier,
                DisplayName = "Style Atelier",
                MonthlyPrice = 79.90m,
                Avatars = 15,
                AvatarTryOnsPerWeek = 140,
                RealisticRendersPerMonth = 80,
                SavedLooks = 9999
            }
        };

        public static AccountPlanDefinition Resolve(string? planId)
        {
            if (!string.IsNullOrWhiteSpace(planId) && Plans.TryGetValue(planId.Trim(), out var plan))
            {
                return plan;
            }

            return Plans[AccountPlanType.Free];
        }

        public static bool IsValid(string? planId)
        {
            return !string.IsNullOrWhiteSpace(planId) && Plans.ContainsKey(planId.Trim());
        }

        public static string GetWeekPeriodKey(DateTime utcNow)
        {
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var week = calendar.GetWeekOfYear(utcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return $"{utcNow.Year}-W{week:D2}";
        }

        public static string GetMonthPeriodKey(DateTime utcNow)
        {
            return $"{utcNow.Year}-{utcNow.Month:D2}";
        }

        public static string GetPeriodKeyForMetric(string metricType, DateTime utcNow)
        {
            return metricType switch
            {
                UsageMetricType.AvatarTryOn => GetWeekPeriodKey(utcNow),
                UsageMetricType.RealisticRender => GetMonthPeriodKey(utcNow),
                UsageMetricType.SharedLook => GetWeekPeriodKey(utcNow),
                UsageMetricType.PurchaseClick => GetWeekPeriodKey(utcNow),
                _ => "lifetime"
            };
        }

        public static int GetLimitForMetric(AccountPlanDefinition plan, string metricType)
        {
            return metricType switch
            {
                UsageMetricType.AvatarTryOn => plan.AvatarTryOnsPerWeek,
                UsageMetricType.RealisticRender => plan.RealisticRendersPerMonth,
                UsageMetricType.SavedLook => plan.SavedLooks,
                UsageMetricType.AvatarSlot => plan.Avatars,
                _ => 0
            };
        }

        public static PlanLimitsResponse ToResponse(AccountPlanDefinition plan)
        {
            return new PlanLimitsResponse
            {
                Avatars = plan.Avatars,
                AvatarTryOnsPerWeek = plan.AvatarTryOnsPerWeek,
                RealisticRendersPerMonth = plan.RealisticRendersPerMonth,
                SavedLooks = plan.SavedLooks
            };
        }
    }
}
