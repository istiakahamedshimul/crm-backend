namespace backend.Services;

public static class KpiFormulas
{
    public static decimal Rate(decimal numerator, decimal denominator) => denominator <= 0 ? 0 : Math.Round(numerator / denominator * 100m, 2);
    public static decimal Average(decimal total, decimal count) => count <= 0 ? 0 : Math.Round(total / count, 2);
    public static decimal Growth(decimal current, decimal previous) => previous == 0 ? (current == 0 ? 0 : 100) : Math.Round((current - previous) / Math.Abs(previous) * 100m, 2);
    public static decimal Achievement(decimal current, decimal target) => Rate(current, target);
    public static decimal PipelineVelocity(decimal activeOpportunities, decimal winRatePercent, decimal averageBookingValue, decimal averageSalesCycleDays) =>
        averageSalesCycleDays <= 0 ? 0 : Math.Round(activeOpportunities * (winRatePercent / 100m) * averageBookingValue / averageSalesCycleDays, 2);
    public static string Status(decimal achievement, bool lowerIsBetter = false)
    {
        if (lowerIsBetter) return achievement <= 100 ? "good" : achievement <= 150 ? "warning" : "critical";
        return achievement >= 90 ? "good" : achievement >= 70 ? "warning" : "critical";
    }
}
