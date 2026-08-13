namespace TaskManager.Bussiness.Authorization
{
    public enum AuthorizationFailureReason
    {
        None,
        NotFound,       // Visibility فشلت → 404
        Forbidden,      // Permission أو Resource Condition فشلت → 403
    }

    public sealed class AuthorizationResult
    {
        public bool Succeeded { get; }
        public AuthorizationFailureReason FailureReason { get; }
        public string? Message { get; }

        private AuthorizationResult(bool succeeded, AuthorizationFailureReason reason, string? message)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            Message = message;
        }

        public static AuthorizationResult Success() =>
            new(true, AuthorizationFailureReason.None, null);

        public static AuthorizationResult NotFound(string message = "Resource not found.") =>
            new(false, AuthorizationFailureReason.NotFound, message);

        public static AuthorizationResult Forbidden(string message = "You are not authorized to perform this action.") =>
            new(false, AuthorizationFailureReason.Forbidden, message);
    }
}
