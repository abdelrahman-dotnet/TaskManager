namespace TaskManager.API.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }

        public string? Message { get; init; }

        public T? Data { get; set; }

        public IReadOnlyList<string>? Errors { get; init; }

        public string? ErrorCode { get; init; }

        public string? CorrelationId { get; init; }

        public static ApiResponse<T> SuccessResult(T? data,string? message = null,string? correlationId = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
                CorrelationId = correlationId
            };

        public static ApiResponse<T> Failure(string message,IEnumerable<string>? errors = null,string? code = null,string? correlationId = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors?.ToList().AsReadOnly(),
                ErrorCode = code,
                CorrelationId = correlationId
            };
    }
}
