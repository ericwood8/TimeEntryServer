namespace TimeEntry.ApiService.Security;

/// <summary> Settings for the tokens the login endpoint hands out (section "Jwt" of the configuration). </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TimeEntry";
    public string Audience { get; set; } = "TimeEntryUI";

    /// <summary> At least 32 characters. Never commit it: use user-secrets or an environment variable (Jwt__Key). </summary>
    public string? Key { get; set; }

    public int ExpiresMinutes { get; set; } = 60;
}
