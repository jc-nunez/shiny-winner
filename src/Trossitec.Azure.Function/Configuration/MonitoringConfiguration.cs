namespace Trossitec.Azure.Function.Configuration;

public class MonitoringConfiguration
{
    public string TimerInterval { get; set; } = "0 */5 * * * *"; // Every 5 minutes
    public int MaxCheckCount { get; set; } = 100;
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(24);
}
