using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TaskManager.API.HealthChecks.Models;
using TaskManager.Data.Context;
using HealthChecks.UI.Client;
namespace TaskManager.API.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        services.AddHealthChecks()

            .AddCheck(
                HealthCheckNames.Self,
                () => HealthCheckResult.Healthy("Application is running"),
                tags: new[] { HealthCheckTags.Live })

            .AddDbContextCheck<AppDbContext>(
                name: HealthCheckNames.SqlServer)

            .AddRedis(
                configuration.GetConnectionString("Redis")!,
                name: HealthCheckNames.Redis);

        var endpoint =
            configuration["APP_URL"] is { Length: > 0 } appUrl
                ? $"{appUrl.TrimEnd('/')}/health"
                : "/health";

        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(
                configuration.GetValue<int>("HealthChecksUI:EvaluationTimeInSeconds"));

            options.MaximumHistoryEntriesPerEndpoint(
                configuration.GetValue<int>("HealthChecksUI:MaximumHistoryEntriesPerEndpoint"));

            options.AddHealthCheckEndpoint(
                "TaskManager API",
                endpoint);
        })
        .AddInMemoryStorage();

        return services;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(
    this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Live),
            ResponseWriter = WriteResponse
        });

        endpoints.MapHealthChecks("/health/details", new HealthCheckOptions
        {
            ResponseWriter = WriteResponse
        });

        return endpoints;
    }
    public static IEndpointRouteBuilder MapHealthCheckDashboard(
    this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api";
        });

        return endpoints;
    }

    private static async Task WriteResponse(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            TotalDurationInMilliseconds = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(entry =>
                new HealthCheckEntryResponse
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    Description = entry.Value.Description
                }).ToList()
        };

        var options = context.RequestServices
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
        
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response, options));
    }
}
