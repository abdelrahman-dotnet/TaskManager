namespace TaskManager.Bussiness.Config
{
    public class JwtSettings
    {
        // FIX: "= null" (rather than "= null!") triggers CS8618 nullable
        // warnings and is inconsistent with the "null!" convention used
        // everywhere else in the project for required-but-bound-later strings.
        public string Key { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public double DurationInMinutes { get; set; }
    }
}
