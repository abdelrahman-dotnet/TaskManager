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

        // ui

        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(30);

            options.MaximumHistoryEntriesPerEndpoint(50);

            options.AddHealthCheckEndpoint(
                "TaskManager API",
                "/health");
        }).AddInMemoryStorage();
        return services;
    }

    public static IEndpointRouteBuilder MapApplicationHealthChecks(
    this IEndpointRouteBuilder endpoints)
    {
        // Endpoint الخاص بالـ HealthChecks UI
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Liveness Probe
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Live),
            ResponseWriter = WriteResponse
        });


        // development not for ui
        endpoints.MapHealthChecks("/health/details", new HealthCheckOptions
        {
            ResponseWriter = WriteResponse
        });

        // HealthChecks Dashboard
        endpoints.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui"; // path
            options.ApiPath = "/health-ui-api"; //api path
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