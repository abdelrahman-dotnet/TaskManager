using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskManager.API.Responses;

public class ApiResponseFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            WrapResult(context, objectResult.Value, objectResult.StatusCode ?? 200);
        }
        else if (context.Result is StatusCodeResult statusResult)
        {
            WrapResult(context, null, statusResult.StatusCode);
        }
        else if (context.Result is EmptyResult)
        {
            WrapResult(context, null, StatusCodes.Status204NoContent);
        }
        else if (context.Result is CreatedResult created)
        {
            WrapResult(context, created.Value, StatusCodes.Status201Created);
        }
        else if (context.Result is CreatedAtActionResult createdAction)
        {
            WrapResult(context, createdAction.Value, StatusCodes.Status201Created);
        }
        else if (context.Result is CreatedAtRouteResult createdRoute)
        {
            WrapResult(context, createdRoute.Value, StatusCodes.Status201Created);
        }
    }

    private void WrapResult(ActionExecutedContext context, object? value, int statusCode)
    {
        var isSuccess = statusCode >= 200 && statusCode < 300;
        ApiResponse<object> wrapped;

        if (value != null &&
             value.GetType().IsGenericType &&
             value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
        {
            return;
        }
        if (statusCode == StatusCodes.Status204NoContent)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status204NoContent);
            return;
        }
        if (isSuccess)
        {
            wrapped = ApiResponse<object>.SuccessResult(value, "Success");
        }
        else
        {
            wrapped = ApiResponse<object>.Failure("Request Failed", null, $"ERR-{statusCode}");
        }

        context.Result = new ObjectResult(wrapped)
        {
            StatusCode = statusCode
        };
    }
}