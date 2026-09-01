namespace TaskManager.API.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public PolicyOptions Global { get; set; } = new();

    public PolicyOptions Login { get; set; } = new();
}

public sealed class PolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; }

    public int QueueLimit { get; set; }
}