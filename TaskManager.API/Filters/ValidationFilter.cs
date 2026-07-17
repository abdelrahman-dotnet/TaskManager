using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskManager.API.Responses;

namespace TaskManager.API.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Distinct()
                    .ToList();

                context.Result = new BadRequestObjectResult(
                    ApiResponse<object>.Failure(
                        message: "Validation failed.",
                        errors: errors,
                        code: "ERR-400"));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action required
        }
    }
}