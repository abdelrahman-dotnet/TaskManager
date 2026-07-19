namespace TaskManager.API.HealthChecks.Models;

public class HealthCheckEntryResponse
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public double Duration { get; set; }

    public string? Description { get; set; }
}