using System.Diagnostics;
using TaskManager.API.Responses;

namespace TaskManager.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next,ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled Exception");
                var statusCode = ex switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status500InternalServerError
                };
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;
                var response = ApiResponse<object>.Failure(
                    message:"Request Failed",
                    errors : new List<string> { ex.Message},
                    code : $"ERR-{statusCode}"
                //context.Response.ContentType = "application/json";
                //context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                //var response = new ApiResponse<object>(
                //    message:"Internal Server Error",
                //    errors:new List<string> { ex.Message }
                );
#if DEBUG
                //response.Data = new
                //{
                //   exception = ex.Message,
                //   StackTrace = ex.StackTrace,
                //   Source = ex.Source
                response.Data = new
                {
                    Data = new
                    {
                        exception = ex.Message,
                        StackTrace = ex.StackTrace,
                        source = ex.Source
                    }
                };
#endif
                await context.Response.WriteAsJsonAsync( response );
            }
        }
    }
}
