using TaskManager.API.Exceptions;
using TaskManager.API.Responses;
namespace TaskManager.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.TraceIdentifier;
                _logger.LogError(
                    ex,
                    "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
                    correlationId,
                    context.Request.Path,
                    context.Request.Method);
                var httpStatusCode = ex switch
                {
                    BadRequestException => StatusCodes.Status400BadRequest,
                    NotFoundException => StatusCodes.Status404NotFound,
                    ForbiddenException => StatusCodes.Status403Forbidden,
                    ConflictException => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status500InternalServerError
                };
                IReadOnlyList<string>? errors = null;
                if (ex is BadRequestException badRequestException)
                {
                    errors = badRequestException.Errors;
                }
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = httpStatusCode;
                var message = httpStatusCode == StatusCodes.Status500InternalServerError
                        ? "An unexpected error occurred."
                        : ex.Message;
                var response = ApiResponse<object>.Failure(
                    message: message,
                    errors: errors,
                    code: $"ERR-{httpStatusCode}",
                    correlationId: correlationId);
#if DEBUG
                response.Data = new
                {
                    /*exception = ex.Message*/ // Will back to this again
                    ex.InnerException?.Message,
                    type = ex.GetType().Name,
                    stackTrace = ex.StackTrace,
                    source = ex.Source
                };
#endif
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}