namespace TaskManager.API.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }   // init منع التعديلات بعد الانشاء
        public string? Message { get; init; }
        public T? Data { get; set; }
        public IReadOnlyList<string>? Errors { get; set; }
        public string? ErrorCode { get; set; }

        public static ApiResponse<T> SuccessResult(T? data, string? message = null)
            => new()
            {
                Success = true,
                Message = message,
                Data = data,
            };

        public static ApiResponse<T> Failure(string message, IEnumerable<string>? errors = null,string? code = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors?.ToList(),
                ErrorCode = code
            };
        //public ApiResponse()
        //{

        //}
        //public ApiResponse(T data,string? message = null)
        //{
        //    Success = true;
        //    Data = data;
        //    Message = message;
        //}
        //public ApiResponse(string message,List<string>? errors = null, string? errorCode = null)
        //{
        //    Success = false;
        //    Message = message;
        //    Errors = errors;
        //    ErrorCode = errorCode;
        //}
    }
}
