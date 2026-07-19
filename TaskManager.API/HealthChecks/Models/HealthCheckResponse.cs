namespace TaskManager.API.HealthChecks.Models;

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;

    public double TotalDurationInMilliseconds { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public List<HealthCheckEntryResponse> Checks { get; set; } = new();
}