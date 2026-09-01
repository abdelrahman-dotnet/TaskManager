using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TaskManager.API.Constants;
using TaskManager.API.Options;
using TaskManager.API.Responses;

namespace TaskManager.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitingOptions =
            configuration
                .GetSection(RateLimitingOptions.SectionName)
                .Get<RateLimitingOptions>()
            ?? throw new InvalidOperationException(
                "RateLimiting configuration is missing.");

        ValidateRateLimitingOptions(rateLimitingOptions);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                RateLimitPolicyNames.Global,
                context =>
                {
                    var userId =
                        context.User.FindFirstValue(
                            ClaimTypes.NameIdentifier);

                    var partitionKey =
                        string.IsNullOrWhiteSpace(userId)
                            ? $"ip:{GetClientIpAddress(context)}"
                            : $"user:{userId}";

                    return RateLimitPartition
                        .GetSlidingWindowLimiter(
                            partitionKey,
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    rateLimitingOptions
                                        .Global
                                        .PermitLimit,

                                Window =
                                    TimeSpan.FromSeconds(
                                        rateLimitingOptions
                                            .Global
                                            .WindowSeconds),

                                SegmentsPerWindow = 6,

                                QueueLimit =
                                    rateLimitingOptions
                                        .Global
                                        .QueueLimit,

                                QueueProcessingOrder =
                                    QueueProcessingOrder.OldestFirst
                            });
                });

            options.AddPolicy(
                RateLimitPolicyNames.Login,
                context =>
                {
                    var partitionKey =
                        $"ip:{GetClientIpAddress(context)}";

                    return RateLimitPartition
                        .GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit =
                                    rateLimitingOptions
                                        .Login
                                        .PermitLimit,

                                Window =
                                    TimeSpan.FromSeconds(
                                        rateLimitingOptions
                                            .Login
                                            .WindowSeconds),

                                QueueLimit =
                                    rateLimitingOptions
                                        .Login
                                        .QueueLimit,

                                QueueProcessingOrder =
                                    QueueProcessingOrder.OldestFirst
                            });
                });

            options.OnRejected = async (
                context,
                cancellationToken) =>
            {
                var httpContext = context.HttpContext;

                var correlationId =
                    httpContext.Items["CorrelationId"]?.ToString();

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(
                            retryAfter.TotalSeconds)
                        .ToString();
                }

                httpContext.Response.StatusCode =
                    StatusCodes.Status429TooManyRequests;

                httpContext.Response.ContentType =
                    "application/json";

                var response =
                    ApiResponse<object?>.Failure(
                        message:
                            "Too many requests. Please try again later.",
                        code:
                            "RATE_LIMIT_EXCEEDED",
                        correlationId:
                            correlationId);

                await httpContext.Response.WriteAsJsonAsync(
                    response,
                    cancellationToken);
            };
        });

        return services;
    }

    private static void ValidateRateLimitingOptions(
        RateLimitingOptions options)
    {
        if (options.Global.PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Global:PermitLimit must be greater than zero.");
        }

        if (options.Global.WindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Global:WindowSeconds must be greater than zero.");
        }

        if (options.Global.QueueLimit < 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Global:QueueLimit cannot be negative.");
        }

        if (options.Login.PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Login:PermitLimit must be greater than zero.");
        }

        if (options.Login.WindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Login:WindowSeconds must be greater than zero.");
        }

        if (options.Login.QueueLimit < 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Login:QueueLimit cannot be negative.");
        }
    }

    private static string GetClientIpAddress(
        HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString()
               ?? "unknown";
    }
}