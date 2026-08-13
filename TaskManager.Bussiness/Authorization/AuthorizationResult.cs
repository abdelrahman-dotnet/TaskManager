namespace TaskManager.Bussiness.Authorization
{
    public enum AuthorizationFailureReason
    {
        None,
        NotFound,       // Visibility failed -> 404
        Forbidden,      // Permission or Resource Condition failed -> 403
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

        /// <summary>
        /// Raises the appropriate domain exception for this failure (404 or 403).
        /// No-op if Succeeded == true.
        /// </summary>
        public void ThrowIfFailed()
        {
            if (Succeeded)
                return;

            var message = Message ?? (
                FailureReason == AuthorizationFailureReason.NotFound
                    ? "Resource not found."
                    : "You are not authorized to perform this action.");

            if (FailureReason == AuthorizationFailureReason.NotFound)
                throw new AuthorizationNotFoundException(message);

            throw new AuthorizationForbiddenException(message);
        }
    }

    /// <summary>Domain exception for pipeline Visibility failures (maps to 404 in API layer).</summary>
    public class AuthorizationNotFoundException : Exception
    {
        public AuthorizationNotFoundException(string message) : base(message) { }
    }

    /// <summary>Domain exception for pipeline Permission/ResourceCondition failures (maps to 403 in API layer).</summary>
    public class AuthorizationForbiddenException : Exception
    {
        public AuthorizationForbiddenException(string message) : base(message) { }
    }
}