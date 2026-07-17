namespace TaskManager.API.Exceptions
{
    public class BadRequestException : Exception
    {
        public IReadOnlyList<string>? Errors { get; }

        public BadRequestException(string message)
            : base(message)
        {
        }

        public BadRequestException(IEnumerable<string> errors)
            : base("Validation failed.")
        {
            Errors = errors.ToList().AsReadOnly();
        }
    }
}